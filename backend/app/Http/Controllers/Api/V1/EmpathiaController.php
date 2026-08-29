<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Models\AccompanimentSession;
use App\Models\ApiToken;
use App\Models\RiskSignalRecord;
use App\Models\Turn;
use App\Models\User;
use App\Services\SessionEventBus;
use App\Services\TurnOrchestrator;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Str;
use Symfony\Component\HttpFoundation\BinaryFileResponse;

class EmpathiaController extends Controller
{
    public function health()
    {
        $dbOk = true;
        try {
            User::query()->limit(1)->get();
        } catch (\Throwable) {
            $dbOk = false;
        }

        $intelOk = false;
        try {
            $intelOk = Http::timeout(2)
                ->withHeaders(['X-Internal-Token' => config('empathia.intelligence_token')])
                ->get(rtrim(config('empathia.intelligence_url'), '/').'/internal/v1/health')
                ->successful();
        } catch (\Throwable) {
            $intelOk = config('empathia.intel_stub') === true;
        }

        if (config('empathia.intel_stub')) {
            $intelOk = true;
        }

        $status = ($dbOk && $intelOk) ? 'ok' : ($dbOk ? 'degraded' : 'down');

        return response()->json([
            'status' => $status,
            'checks' => [
                'db' => $dbOk,
                'intelligence' => $intelOk,
                'intel_stub' => (bool) config('empathia.intel_stub'),
            ],
        ]);
    }

    public function login(Request $request)
    {
        $data = $request->validate([
            'username' => 'required|string',
            'password' => 'required|string',
        ]);

        $user = User::query()->where('username', $data['username'])->first();
        if (! $user || ! Hash::check($data['password'], $user->password)) {
            return response()->json([
                'error' => ['code' => 'INVALID_CREDENTIALS', 'message' => 'Invalid username or password'],
            ], 401);
        }

        $plain = Str::random(48);
        ApiToken::query()->create([
            'id' => (string) Str::uuid(),
            'user_id' => $user->id,
            'token' => hash('sha256', $plain),
            'expires_at' => now()->addDays(7),
        ]);

        // Laboratorio: al login, cerrar sesiones activas para no bloquear a A.
        // Importante: no usar STDERR (en namespace falla como constante indefinida → HTTP 500).
        $closed = 0;
        try {
            $closed = AccompanimentSession::query()
                ->where('status', 'active')
                ->update([
                    'status' => 'closed',
                    'ended_at' => now(),
                ]);
            if ($closed > 0) {
                logger()->info('[B] Login '.$user->username.' — sesiones activas cerradas: '.$closed);
                self::labLog('[B] Login '.$user->username.' — sesiones activas cerradas: '.$closed);
            }
        } catch (\Throwable $e) {
            report($e);
        }

        self::labLog('[B] CONEXIÓN / Login OK user='.$user->username);

        return response()->json([
            'token' => $plain,
            'token_type' => 'Bearer',
            'expires_at' => now()->addDays(7)->utc()->toIso8601String(),
            'user' => $this->userPayload($user),
            'closed_active_sessions' => $closed,
        ]);
    }

    public function logout(Request $request)
    {
        $header = $request->header('Authorization', '');
        $plain = str_starts_with($header, 'Bearer ') ? substr($header, 7) : '';
        if ($plain !== '') {
            ApiToken::query()->where('token', hash('sha256', $plain))->delete();
        }

        return response()->json(['ok' => true]);
    }

    public function me(Request $request)
    {
        return response()->json(['user' => $this->userPayload($request->user())]);
    }

    public function createSession(Request $request, SessionEventBus $events)
    {
        $user = $request->user();
        if ($user->role !== 'student' && $user->role !== 'admin') {
            return response()->json([
                'error' => ['code' => 'FORBIDDEN', 'message' => 'Only students can start accompaniment sessions'],
            ], 403);
        }

        $data = $request->validate([
            'locale' => 'sometimes|in:es',
            'client' => 'sometimes|in:unity',
        ]);

        $studentId = $user->role === 'admin'
            ? User::query()->where('role', 'student')->value('id')
            : $user->id;

        if (empty($studentId)) {
            return response()->json([
                'error' => [
                    'code' => 'VALIDATION_ERROR',
                    'message' => 'No student user available to attach the session',
                ],
            ], 422);
        }

        try {
            // Laboratorio: cerrar sesión activa previa (si existe) para no bloquear a A.
            $active = AccompanimentSession::query()->where('status', 'active')->first();
            if ($active) {
                $active->status = 'closed';
                $active->ended_at = now();
                $active->save();
                try {
                    $events->push($active, 'session.closed', ['reason' => 'replaced']);
                    $events->push($active, 'session.state', ['state' => 'closed']);
                } catch (\Throwable $e) {
                    // No tumbar createSession si el event bus falla al cerrar.
                    report($e);
                }
            }

            $session = AccompanimentSession::query()->create([
                'id' => (string) Str::uuid(),
                'student_user_id' => $studentId,
                'status' => 'active',
                'locale' => $data['locale'] ?? 'es',
                'client' => $data['client'] ?? 'unity',
                'ws_ticket' => Str::random(40),
                'started_at' => now(),
            ]);

            $events->push($session, 'session.ready', [
                'session_id' => $session->id,
                'student_user_id' => (string) $session->student_user_id,
                'locale' => $session->locale,
            ]);
            $events->push($session, 'session.state', ['state' => 'idle']);

            logger()->info('[B] Nueva sesión activa id='.$session->id);
            self::labLog('[B] Nueva sesión activa id='.$session->id);

            $startedAt = $session->started_at
                ? $session->started_at->utc()->toIso8601String()
                : now('UTC')->toIso8601String();

            return response()->json([
                'session' => [
                    'id' => $session->id,
                    'student_user_id' => (string) $session->student_user_id,
                    'status' => $session->status,
                    'locale' => $session->locale,
                    'client' => $session->client,
                    'started_at' => $startedAt,
                    'ws_url' => 'ws://127.0.0.1:8000/ws/v1/accompaniment/'.$session->id,
                    'ws_ticket' => $session->ws_ticket,
                ],
            ], 201);
        } catch (\Throwable $e) {
            report($e);

            return response()->json([
                'error' => [
                    'code' => 'INTERNAL_ERROR',
                    'message' => 'createSession failed: '.$e->getMessage(),
                ],
            ], 500);
        }
    }

    public function getActiveSession(Request $request)
    {
        $active = AccompanimentSession::query()->where('status', 'active')->first();
        if (!$active) {
            return response()->json(['session' => null]);
        }

        // Lab: cualquier usuario autenticado puede leer el id activo (desbloquea a A).
        return response()->json([
            'session' => [
                'id' => $active->id,
                'status' => $active->status,
                'student_user_id' => (string) $active->student_user_id,
            ],
        ]);
    }

    public function closeActiveSession(Request $request, SessionEventBus $events)
    {
        $active = AccompanimentSession::query()->where('status', 'active')->first();
        if (!$active) {
            return response()->json(['ok' => true, 'closed' => false, 'message' => 'No active session']);
        }

        try {
            $active->status = 'closed';
            $active->ended_at = now();
            $active->save();
            try {
                $events->push($active, 'session.closed', ['reason' => 'user']);
                $events->push($active, 'session.state', ['state' => 'closed']);
            } catch (\Throwable $e) {
                report($e);
            }

            return response()->json([
                'ok' => true,
                'closed' => true,
                'session' => ['id' => $active->id, 'status' => 'closed'],
            ]);
        } catch (\Throwable $e) {
            report($e);

            return response()->json([
                'error' => [
                    'code' => 'INTERNAL_ERROR',
                    'message' => 'closeActiveSession failed: '.$e->getMessage(),
                ],
            ], 500);
        }
    }

    /**
     * Recibe texto del cliente A (alias active o UUID).
     * Body: { "text": "...", "client_turn_key": "<uuid>" } (message también aceptado).
     * Nota: en el B de lab en LAN, client_turn_key es obligatorio.
     */
    public function postSessionText(Request $request, string $sessionId = 'active')
    {
        $data = $request->validate([
            'text' => 'sometimes|string|max:5000',
            'message' => 'sometimes|string|max:5000',
            'client_turn_key' => 'sometimes|uuid',
        ]);

        $message = trim((string) ($data['text'] ?? $data['message'] ?? ''));
        if ($message === '') {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'text/message is required'],
            ], 422);
        }

        $session = $sessionId === 'active'
            ? AccompanimentSession::query()->where('status', 'active')->first()
            : AccompanimentSession::query()->find($sessionId);

        if (! $session || $session->status !== 'active') {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'No active session'],
            ], 422);
        }

        logger()->info('[A→B TEXTO] session='.$session->id.' | '.$message);
        self::labLog('[A→B TEXTO] session='.$session->id.' | '.$message);

        $turnKey = $data['client_turn_key'] ?? (string) Str::uuid();
        $sequence = (int) Turn::query()->where('session_id', $session->id)->max('sequence_no') + 1;
        $turnId = (string) Str::uuid();

        $turn = Turn::query()->create([
            'id' => $turnId,
            'session_id' => $session->id,
            'sequence_no' => $sequence,
            'client_turn_key' => $turnKey,
            'status' => 'accepted',
            'transcript' => $message,
            'reply_text' => 'Recibí tu mensaje: '.$message,
        ]);

        return response()->json([
            'ok' => true,
            'session_id' => $session->id,
            'received_text' => $message,
            'reply_text' => $turn->reply_text,
            'transcript' => $message,
            'turn' => [
                'id' => $turn->id,
                'session_id' => $turn->session_id,
                'sequence_no' => $turn->sequence_no,
                'status' => $turn->status,
                'client_turn_key' => $turn->client_turn_key,
            ],
        ], 202);
    }

    public function getSession(Request $request, string $sessionId)
    {
        $session = AccompanimentSession::query()->findOrFail($sessionId);
        $this->assertCanReadSession($request->user(), $session);

        return response()->json(['session' => $session]);
    }

    public function closeSession(Request $request, string $sessionId, SessionEventBus $events)
    {
        $session = AccompanimentSession::query()->findOrFail($sessionId);
        $this->assertCanWriteSession($request->user(), $session);

        $session->status = 'closed';
        $session->ended_at = now();
        $session->save();

        $events->push($session, 'session.closed', ['reason' => 'user']);
        $events->push($session, 'session.state', ['state' => 'closed']);

        return response()->json(['ok' => true, 'session' => $session]);
    }

    public function createTurn(Request $request, string $sessionId, TurnOrchestrator $orchestrator, SessionEventBus $events)
    {
        $session = AccompanimentSession::query()->findOrFail($sessionId);
        $this->assertCanWriteSession($request->user(), $session);

        if ($session->status !== 'active') {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'Session is not active'],
            ], 422);
        }

        $request->validate([
            'audio' => 'required|file',
            'client_turn_key' => 'required|uuid',
            'sequence_hint' => 'sometimes|integer',
        ]);

        $existing = Turn::query()
            ->where('session_id', $session->id)
            ->where('client_turn_key', $request->input('client_turn_key'))
            ->first();

        if ($existing) {
            return response()->json([
                'turn' => [
                    'id' => $existing->id,
                    'session_id' => $existing->session_id,
                    'sequence_no' => $existing->sequence_no,
                    'status' => $existing->status,
                    'client_turn_key' => $existing->client_turn_key,
                ],
            ], 202);
        }

        $sequence = (int) Turn::query()->where('session_id', $session->id)->max('sequence_no') + 1;
        $turnId = (string) Str::uuid();

        $dir = rtrim(config('empathia.data_root'), DIRECTORY_SEPARATOR)
            .DIRECTORY_SEPARATOR.'audio'.DIRECTORY_SEPARATOR.'input'
            .DIRECTORY_SEPARATOR.$session->id;
        if (! is_dir($dir)) {
            mkdir($dir, 0777, true);
        }
        $audioPath = $dir.DIRECTORY_SEPARATOR.$turnId.'.wav';
        $request->file('audio')->move($dir, $turnId.'.wav');

        self::labLog('[A→B AUDIO] session='.$session->id.' turn='.$turnId.' | wav recibido, convirtiendo…');
        logger()->info('[A→B AUDIO] session='.$session->id.' turn='.$turnId.' | wav recibido');

        $turn = Turn::query()->create([
            'id' => $turnId,
            'session_id' => $session->id,
            'sequence_no' => $sequence,
            'client_turn_key' => $request->input('client_turn_key'),
            'status' => 'accepted',
        ]);

        $events->push($session, 'turn.accepted', [
            'turn_id' => $turn->id,
            'sequence_no' => $turn->sequence_no,
            'client_turn_key' => $turn->client_turn_key,
        ]);

        try {
            $orchestrator->processAcceptedTurn($turn, $session, $audioPath);
        } catch (\Throwable $e) {
            $turn->status = 'error';
            $turn->save();
            $events->push($session, 'turn.error', [
                'turn_id' => $turn->id,
                'code' => 'INTERNAL_ERROR',
                'message' => $e->getMessage(),
                'retryable' => true,
            ]);
        }

        return response()->json([
            'turn' => [
                'id' => $turn->id,
                'session_id' => $turn->session_id,
                'sequence_no' => $turn->sequence_no,
                'status' => 'accepted',
                'client_turn_key' => $turn->client_turn_key,
            ],
        ], 202);
    }

    public function events(Request $request, string $sessionId, SessionEventBus $bus)
    {
        $session = AccompanimentSession::query()->findOrFail($sessionId);
        $this->assertCanReadSession($request->user(), $session);

        $after = (int) $request->query('after', 0);
        $rows = $session->events()->where('id', '>', $after)->orderBy('id')->limit(100)->get();

        return response()->json([
            'events' => $rows->map(fn ($e) => $bus->envelope($e))->values(),
            'next_after' => $rows->last()->id ?? $after,
        ]);
    }

    public function ttsAudio(Request $request, string $turnId): BinaryFileResponse|\Illuminate\Http\JsonResponse
    {
        $turn = Turn::query()->findOrFail($turnId);
        $session = $turn->session;
        $this->assertCanReadSession($request->user(), $session);

        if (! $turn->tts_path || ! is_file($turn->tts_path)) {
            return response()->json([
                'error' => ['code' => 'NOT_FOUND', 'message' => 'TTS audio missing'],
            ], 404);
        }

        return response()->file($turn->tts_path, [
            'Content-Type' => 'audio/wav',
        ]);
    }

    public function riskSignals(Request $request)
    {
        $user = $request->user();
        if (! in_array($user->role, ['counselor', 'admin'], true)) {
            return response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Forbidden']], 403);
        }

        $q = RiskSignalRecord::query()->orderByDesc('created_at');
        if ($request->filled('student_id')) {
            $q->where('student_user_id', $request->query('student_id'));
        }

        return response()->json(['data' => $q->limit(100)->get()]);
    }

    public function riskCatalog()
    {
        $path = base_path('../contratos/riesgo/v0/codes.json');

        return response()->json(json_decode(file_get_contents($path), true));
    }

    public function students(Request $request)
    {
        $user = $request->user();
        if (! in_array($user->role, ['counselor', 'admin'], true)) {
            return response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Forbidden']], 403);
        }

        $students = User::query()->where('role', 'student')->get(['id', 'username', 'display_name', 'role']);

        return response()->json(['data' => $students]);
    }

    private function userPayload(User $user): array
    {
        return [
            'id' => (string) $user->id,
            'display_name' => $user->display_name ?? $user->name,
            'role' => $user->role,
            'username' => $user->username,
        ];
    }

    /** Alias lab. */
    private function console(string $message): void
    {
        self::labLog($message);
    }

    /**
     * Lab: laravel.log + lab-terminal.log (+ intento stdout/stderr).
     * En Windows usa: powershell -File serve-lab.ps1
     */
    public static function labLog(string $message): void
    {
        \App\Support\LabTerminal::write($message);
    }

    private function assertCanReadSession(User $user, AccompanimentSession $session): void
    {
        if (in_array($user->role, ['counselor', 'admin'], true)) {
            return;
        }
        if ((int) $user->id !== (int) $session->student_user_id) {
            abort(response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Forbidden']], 403));
        }
    }

    private function assertCanWriteSession(User $user, AccompanimentSession $session): void
    {
        if ($user->role === 'admin') {
            return;
        }
        if ($user->role !== 'student' || (int) $user->id !== (int) $session->student_user_id) {
            abort(response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Forbidden']], 403));
        }
    }
}
