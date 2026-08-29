<?php

namespace App\Services;

use App\Models\AccompanimentSession;
use App\Models\RiskSignalRecord;
use App\Models\Turn;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Str;
use RuntimeException;

class TurnOrchestrator
{
    public function __construct(private SessionEventBus $events) {}

    public function processAcceptedTurn(Turn $turn, AccompanimentSession $session, string $audioAbsolutePath): void
    {
        $this->events->push($session, 'session.state', ['state' => 'processing']);
        $this->events->push($session, 'turn.processing', [
            'turn_id' => $turn->id,
            'stage' => 'stt',
        ]);

        $inference = config('empathia.intel_stub')
            ? $this->stubInference($turn, $session)
            : $this->callIntelligence($turn, $session, $audioAbsolutePath);

        $this->persistInference($turn, $session, $inference);

        $transcript = (string) ($turn->transcript ?? '');
        \App\Http\Controllers\Api\V1\EmpathiaController::labLog(
            '[A→B AUDIO→TEXTO] session='.$session->id.' turn='.$turn->id.' | '.$transcript
        );

        if (config('empathia.audio_retention') === 'R1' && is_file($audioAbsolutePath)) {
            @unlink($audioAbsolutePath);
        }

        $ttsUrl = url('/api/v1/accompaniment/turns/'.$turn->id.'/audio/tts');

        $this->events->push($session, 'turn.result', [
            'turn_id' => $turn->id,
            'sequence_no' => $turn->sequence_no,
            'transcript' => $turn->transcript,
            'emotion' => [
                'label' => $turn->emotion_label,
                'confidence' => $turn->emotion_confidence,
            ],
            'reply_text' => $turn->reply_text,
            'tts' => [
                'format' => 'wav',
                'url' => $ttsUrl,
            ],
            'expression' => $turn->expression_packet,
            'risk_emitted' => $turn->risk_emitted,
            'metrics' => [
                'total_ms' => $turn->metrics['total_ms'] ?? 0,
            ],
        ]);

        $this->events->push($session, 'session.state', ['state' => 'speaking']);
    }

    private function stubInference(Turn $turn, AccompanimentSession $session): array
    {
        $fixturePath = base_path('../expresion/fixtures/paquete-expresion-ejemplo.json');
        $expression = is_file($fixturePath)
            ? json_decode(file_get_contents($fixturePath), true)
            : ['version' => 1, 'lips' => [], 'face' => []];
        $expression['turn_id'] = $turn->id;

        $outDir = rtrim(config('empathia.data_root'), DIRECTORY_SEPARATOR)
            .DIRECTORY_SEPARATOR.'audio'.DIRECTORY_SEPARATOR.'output'
            .DIRECTORY_SEPARATOR.$session->id;
        if (! is_dir($outDir)) {
            mkdir($outDir, 0777, true);
        }
        $outPath = $outDir.DIRECTORY_SEPARATOR.$turn->id.'.wav';
        $this->writeSilentWav($outPath);

        return [
            'transcript' => ['text' => 'Hola, hoy me siento un poco cansado pero quiero hablar.', 'confidence' => 0.9],
            'emotion' => ['label' => 'sadness', 'confidence' => 0.62],
            'risk_signals' => [],
            'reply' => [
                'text' => 'Gracias por contármelo. Estoy aquí para acompañarte. ¿Quieres contarme un poco más sobre cómo te ha ido el día?',
                'guardrail_flags' => [],
            ],
            'tts' => ['path' => $outPath, 'format' => 'wav', 'duration_ms' => $expression['duration_ms'] ?? 2400],
            'timing' => ['quality' => 'low', 'cues' => []],
            'expression' => $expression,
            'memory' => ['updated' => true],
            'model_versions' => ['stt' => 'stub', 'llm' => 'stub', 'tts' => 'stub'],
            'metrics' => ['stt_ms' => 10, 'analysis_ms' => 5, 'llm_ms' => 10, 'tts_ms' => 10, 'total_ms' => 35],
        ];
    }

    private function callIntelligence(Turn $turn, AccompanimentSession $session, string $audioAbsolutePath): array
    {
        $base = rtrim(config('empathia.intelligence_url'), '/');
        $response = Http::timeout(120)
            ->withHeaders(['X-Internal-Token' => config('empathia.intelligence_token')])
            ->post($base.'/internal/v1/infer/turn', [
                'request_id' => (string) Str::uuid(),
                'session_id' => $session->id,
                'turn_id' => $turn->id,
                'student_id' => (string) $session->student_user_id,
                'locale' => 'es',
                'audio' => ['path' => $audioAbsolutePath],
                'options' => ['return_partials' => false, 'max_latency_ms' => 120000],
            ]);

        if (! $response->successful()) {
            throw new RuntimeException('INTELLIGENCE_UNAVAILABLE');
        }

        return $response->json();
    }

    private function persistInference(Turn $turn, AccompanimentSession $session, array $inference): void
    {
        $expression = $inference['expression'] ?? null;
        if (! $expression && isset($inference['timing'], $inference['emotion'])) {
            $fixturePath = base_path('../expresion/fixtures/paquete-expresion-ejemplo.json');
            $expression = is_file($fixturePath)
                ? json_decode(file_get_contents($fixturePath), true)
                : [];
            $expression['turn_id'] = $turn->id;
            $expression['emotion_drive'] = [
                'label' => $inference['emotion']['label'] ?? 'neutral',
                'intensity' => $inference['emotion']['confidence'] ?? 0.5,
            ];
            $expression['timing_quality'] = $inference['timing']['quality'] ?? 'low';
        }

        $riskSignals = $inference['risk_signals'] ?? [];
        $catalog = $this->riskCatalogCodes();

        $turn->fill([
            'status' => 'completed',
            'transcript' => $inference['transcript']['text'] ?? '',
            'reply_text' => $inference['reply']['text'] ?? '',
            'emotion_label' => $inference['emotion']['label'] ?? 'neutral',
            'emotion_confidence' => $inference['emotion']['confidence'] ?? null,
            'tts_path' => $inference['tts']['path'] ?? null,
            'expression_packet' => $expression,
            'model_versions' => $inference['model_versions'] ?? [],
            'metrics' => $inference['metrics'] ?? [],
            'risk_emitted' => count($riskSignals) > 0,
        ])->save();

        foreach ($riskSignals as $signal) {
            $code = $signal['code'] ?? 'OTHER';
            if (! in_array($code, $catalog, true)) {
                $code = 'OTHER';
            }
            if (empty($signal['evidence'])) {
                continue;
            }
            RiskSignalRecord::query()->create([
                'id' => (string) Str::uuid(),
                'turn_id' => $turn->id,
                'session_id' => $session->id,
                'student_user_id' => $session->student_user_id,
                'code' => $code,
                'severity' => $signal['severity'] ?? 'low',
                'evidence' => $signal['evidence'],
                'confidence' => $signal['confidence'] ?? 0.5,
                'source' => 'intelligence_v1',
            ]);
        }
    }

    private function riskCatalogCodes(): array
    {
        $path = base_path('../contratos/riesgo/v0/codes.json');
        if (! is_file($path)) {
            return ['OTHER'];
        }
        $json = json_decode(file_get_contents($path), true);

        return array_map(fn ($c) => $c['code'], $json['codes'] ?? []);
    }

    private function writeSilentWav(string $path): void
    {
        $sampleRate = 16000;
        $numSamples = intdiv($sampleRate, 4);
        $data = str_repeat("\x00\x00", $numSamples);
        $dataSize = strlen($data);
        $header = pack(
            'a4Va4a4VvvVVvva4V',
            'RIFF',
            36 + $dataSize,
            'WAVE',
            'fmt ',
            16,
            1,
            1,
            $sampleRate,
            $sampleRate * 2,
            2,
            16,
            'data',
            $dataSize
        );
        file_put_contents($path, $header.$data);
    }
}
