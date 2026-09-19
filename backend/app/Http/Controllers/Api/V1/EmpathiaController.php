<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Models\AccompanimentSession;
use App\Models\ApiToken;
use App\Models\RiskSignalRecord;
use App\Models\StudentProfile;
use App\Models\Turn;
use App\Models\User;
use App\Services\SessionEventBus;
use App\Services\TurnOrchestrator;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;
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
        $intelError = null;
        $intelUrl = rtrim((string) config('empathia.intelligence_url'), '/').'/internal/v1/health';
        try {
            $intelResponse = Http::timeout(5)
                ->connectTimeout(3)
                ->withHeaders(['X-Internal-Token' => config('empathia.intelligence_token')])
                ->get($intelUrl);
            $intelOk = $intelResponse->successful();
            if (! $intelOk) {
                $intelError = 'HTTP '.$intelResponse->status();
            }
        } catch (\Throwable $e) {
            $intelError = $e->getMessage();
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
            'intelligence_url' => $intelUrl,
            'intelligence_error' => $intelError,
        ]);
    }

    public function login(Request $request, SessionEventBus $events)
    {
        $data = $request->validate([
            'username' => 'required|string',
            'password' => 'required|string',
        ]);

        $user = User::query()->where('username', $data['username'])->first();
        if (
            ! $user
            || $user->password === null
            || ! Hash::check($data['password'], $user->password)
        ) {
            return response()->json([
                'error' => ['code' => 'INVALID_CREDENTIALS', 'message' => 'Invalid username or password'],
            ], 401);
        }

        // Solo cierra sesiones de ESE estudiante; no toca las de otros perfiles.
        $closed = $user->role === 'student'
            ? $this->abortActiveSessionsForStudent($events, (int) $user->id, 'login_refresh')
            : 0;
        $this->console('[B] Login '.$user->username.' — sesiones propias cerradas: '.$closed);

        $plain = Str::random(48);
        ApiToken::query()->create([
            'id' => (string) Str::uuid(),
            'user_id' => $user->id,
            'token' => hash('sha256', $plain),
            'expires_at' => now()->addDays(7),
        ]);

        return response()->json([
            'token' => $plain,
            'token_type' => 'Bearer',
            'expires_at' => now()->addDays(7)->utc()->toIso8601String(),
            'user' => $this->userPayload($user),
            'closed_active_sessions' => $closed,
        ]);
    }

    /**
     * Ingreso Unity (pestaña Estudiante): nombre + documento.
     * Si el documento ya tiene perfil activo → valida y entra.
     * Si no existe → registra un perfil nuevo (A puede dar de alta sin admin).
     */
    public function studentIdentify(Request $request, SessionEventBus $events)
    {
        $data = $this->validateStudentSchoolFields($request, requireSchoolExtras: false);
        $documento = $this->normalizeDocumento($data['documento_numero']);

        $profile = StudentProfile::query()
            ->with('user')
            ->where('documento_numero', $documento)
            ->first();

        if ($profile) {
            if (! $profile->is_active || ! $profile->user || $profile->user->role !== 'student') {
                return response()->json([
                    'error' => [
                        'code' => 'INVALID_STUDENT_IDENTITY',
                        'message' => 'Student profile exists but is inactive',
                    ],
                ], 401);
            }

            if (! $this->studentIdentityMatches($profile, $data)) {
                return response()->json([
                    'error' => [
                        'code' => 'INVALID_STUDENT_IDENTITY',
                        'message' => 'nombre does not match the active profile for this documento',
                    ],
                ], 401);
            }

            $this->refreshProfileFromSchoolFields($profile, $data, $documento);
            $this->console('[B] Student identify profile_id='.$profile->id.' doc='.$documento);

            return $this->issueStudentTokenResponse($profile->user->fresh(), $profile->fresh(), $events, 'student_identify');
        }

        // Ingreso sin perfil previo: exige los mismos campos completos del registro.
        foreach (['grado', 'sede', 'jornada'] as $field) {
            if (! filled($data[$field] ?? null)) {
                return response()->json([
                    'error' => [
                        'code' => 'VALIDATION_ERROR',
                        'message' => 'Profile not found. To register send nombre, numero_documento, grado, sede and jornada (or use /auth/student-register).',
                        'fields' => ['grado', 'sede', 'jornada'],
                    ],
                ], 422);
            }
        }

        $profile = $this->createStudentProfileFromSchoolFields($data, $documento, createdBy: null);
        $this->console('[B] Student identify+register profile_id='.$profile->id.' doc='.$documento);

        return $this->issueStudentTokenResponse($profile->user, $profile, $events, 'student_identify_register');
    }

    /**
     * Alta explícita desde Unity (botón Registrarse).
     * Obligatorios: nombre, numero_documento, grado, sede, jornada.
     */
    public function studentRegister(Request $request, SessionEventBus $events)
    {
        $data = $this->validateStudentSchoolFields($request, requireSchoolExtras: true);
        $documento = $this->normalizeDocumento($data['documento_numero']);

        if (StudentProfile::query()->where('documento_numero', $documento)->exists()) {
            return response()->json([
                'error' => [
                    'code' => 'STUDENT_ALREADY_REGISTERED',
                    'message' => 'documento_numero already registered; use student-identify to enter',
                ],
            ], 422);
        }

        $profile = $this->createStudentProfileFromSchoolFields($data, $documento, createdBy: null);
        $this->console('[B] Student register profile_id='.$profile->id.' doc='.$documento);

        return $this->issueStudentTokenResponse($profile->user, $profile, $events, 'student_register');
    }

    /**
     * Ingreso alternativo con código temporal regenerado por el admin.
     */
    public function studentAccess(Request $request, SessionEventBus $events)
    {
        $data = $request->validate([
            'access_code' => 'required|string|max:32',
        ]);

        $code = strtoupper(trim($data['access_code']));
        $profile = StudentProfile::query()
            ->with('user')
            ->where('is_active', true)
            ->whereRaw('UPPER(access_code) = ?', [$code])
            ->first();

        if (! $profile || ! $profile->user || $profile->user->role !== 'student') {
            return response()->json([
                'error' => ['code' => 'INVALID_ACCESS_CODE', 'message' => 'Invalid or inactive access code'],
            ], 401);
        }

        $this->console('[B] Student access_code ok profile='.$profile->id);

        return $this->issueStudentTokenResponse($profile->user, $profile, $events, 'student_access');
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

        if ($user->role === 'admin') {
            $studentId = $request->integer('student_user_id') ?: null;
            if (! $studentId) {
                return response()->json([
                    'error' => [
                        'code' => 'VALIDATION_ERROR',
                        'message' => 'Admin must pass student_user_id, or use assume / student-identify first',
                    ],
                ], 422);
            }
            $target = User::query()->where('id', $studentId)->where('role', 'student')->first();
            if (! $target) {
                return response()->json([
                    'error' => ['code' => 'NOT_FOUND', 'message' => 'student_user_id is not a student'],
                ], 404);
            }
        } else {
            $studentId = $user->id;
        }

        try {
            // Solo cierra sesiones previas de ESTE estudiante (no las de otros perfiles).
            $this->abortActiveSessionsForStudent($events, (int) $studentId, 'replaced_on_create');

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

            $this->console('[B] Nueva sesión activa id='.$session->id);

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
        $user = $request->user();
        $q = AccompanimentSession::query()->where('status', 'active')->orderByDesc('started_at');
        if ($user->role === 'student') {
            $q->where('student_user_id', $user->id);
        }
        $active = $q->first();
        if (! $active) {
            return response()->json(['session' => null]);
        }

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
        $user = $request->user();
        $q = AccompanimentSession::query()->where('status', 'active')->orderByDesc('started_at');
        if ($user->role === 'student') {
            $q->where('student_user_id', $user->id);
        }
        $active = $q->first();
        if (! $active) {
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
     * Alias Unity: acepta text/message y client_turn_key opcional, luego orquesta vía C.
     * Soporta /sessions/active/text (sin {sessionId}) y /sessions/{sessionId}/text.
     */
    public function postSessionText(Request $request, TurnOrchestrator $orchestrator, SessionEventBus $events, ?string $sessionId = null)
    {
        $sessionId = $sessionId ?: 'active';

        if (! $request->filled('text') && $request->filled('message')) {
            $request->merge(['text' => $request->input('message')]);
        }
        if (! $request->filled('client_turn_key')) {
            $request->merge(['client_turn_key' => (string) Str::uuid()]);
        }

        return $this->createTextTurn($request, $sessionId, $orchestrator, $events);
    }

    public function getSession(Request $request, string $sessionId)
    {
        $session = $this->resolveSession($sessionId);
        $this->assertCanReadSession($request->user(), $session);

        return response()->json(['session' => $session]);
    }

    public function getSessionSummary(Request $request, string $sessionId)
    {
        $session = $this->resolveSession($sessionId);
        $this->assertCanReadSession($request->user(), $session);

        $turns = $session->turns()
            ->where('status', 'completed')
            ->get(['emotion_label']);

        return response()->json([
            'summary' => [
                'session_id' => $session->id,
                'status' => $session->status,
                'conversation_summary' => $session->conversation_summary,
                'turn_count' => $turns->count(),
                'emotions' => $turns->groupBy('emotion_label')->map->count()->filter(
                    fn (int $count, ?string $label): bool => $label !== null,
                ),
                'started_at' => $session->started_at?->toISOString(),
                'ended_at' => $session->ended_at?->toISOString(),
            ],
        ]);
    }

    public function closeSession(Request $request, string $sessionId, SessionEventBus $events)
    {
        $session = $this->resolveSession($sessionId);
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
        $session = $this->resolveSession($sessionId);
        $this->assertCanWriteSession($request->user(), $session);

        if ($session->status !== 'active') {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'Session is not active'],
            ], 422);
        }

        $request->validate([
            'audio' => 'required|file',
            'client_turn_key' => 'required|uuid',
            'preferred_name' => 'nullable|string|max:80',
            'sequence_hint' => 'sometimes|integer',
        ]);

        if ($request->filled('preferred_name')) {
            $request->merge([
                'preferred_name' => $this->normalizePreferredName($request->input('preferred_name')),
            ]);
        }

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
            $orchestrator->processAcceptedTurn($turn, $session, $audioPath, $request->input('preferred_name'));
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

    public function createTextTurn(Request $request, string $sessionId, TurnOrchestrator $orchestrator, SessionEventBus $events)
    {
        $this->console('[A→B TEXTO] petición recibida path='.$sessionId.' body='.$request->getContent());

        $session = $this->resolveSession($sessionId);
        $this->assertCanWriteSession($request->user(), $session);

        if ($session->status !== 'active') {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'Session is not active'],
            ], 422);
        }

        // A a veces manda el nombre completo; C solo acepta 1–2 palabras.
        if ($request->filled('preferred_name')) {
            $request->merge([
                'preferred_name' => $this->normalizePreferredName($request->input('preferred_name')),
            ]);
        }

        $data = $request->validate([
            'text' => 'required|string|min:1|max:5000',
            'client_turn_key' => 'required|uuid',
            'preferred_name' => 'nullable|string|max:40',
        ]);

        $this->console('[A→B TEXTO] session='.$session->id.' | '.$data['text'].' preferred_name='.($data['preferred_name'] ?? ''));

        $existing = Turn::query()
            ->where('session_id', $session->id)
            ->where('client_turn_key', $data['client_turn_key'])
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

        $turn = Turn::query()->create([
            'id' => $turnId,
            'session_id' => $session->id,
            'sequence_no' => $sequence,
            'client_turn_key' => $data['client_turn_key'],
            'status' => 'accepted',
            'transcript' => $data['text'],
        ]);

        $events->push($session, 'turn.accepted', [
            'turn_id' => $turn->id,
            'sequence_no' => $turn->sequence_no,
            'client_turn_key' => $turn->client_turn_key,
            'source' => 'text',
        ]);

        try {
            $orchestrator->processTextTurn($turn, $session, $data['text'], $data['preferred_name'] ?? null);
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

        $turn->refresh();

        return response()->json([
            'ok' => true,
            'session_id' => $session->id,
            'received_text' => $data['text'],
            'reply_text' => $turn->reply_text,
            'transcript' => $turn->transcript ?? $data['text'],
            'emotion' => [
                'label' => $turn->emotion_label,
                'confidence' => $turn->emotion_confidence,
            ],
            'turn' => [
                'id' => $turn->id,
                'session_id' => $turn->session_id,
                'sequence_no' => $turn->sequence_no,
                'status' => $turn->status,
                'client_turn_key' => $turn->client_turn_key,
            ],
        ], 202);
    }

    public function events(Request $request, string $sessionId, SessionEventBus $bus)
    {
        $session = $this->resolveSession($sessionId);
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

        // Lista operativa para Unity: solo perfiles activos (Fase 3).
        $rows = StudentProfile::query()
            ->with('user')
            ->where('is_active', true)
            ->whereHas('user', fn ($q) => $q->where('role', 'student'))
            ->orderBy('nombre_preferencia')
            ->limit(200)
            ->get()
            ->map(fn (StudentProfile $profile) => [
                'id' => (string) $profile->user_id,
                'profile_id' => $profile->id,
                'display_name' => $profile->resolvedDisplayName(),
                'nombre_preferencia' => $profile->nombre_preferencia,
                'nombres' => $profile->nombres,
                'apellidos' => $profile->apellidos,
                'grado' => $profile->grado,
                'edad' => $profile->edad,
                'sede' => $profile->sede,
                'jornada' => $profile->jornada,
                'documento_numero' => $profile->documento_numero,
                'role' => 'student',
            ]);

        return response()->json(['data' => $rows]);
    }

    /**
     * Adulto (admin/counselor) asume la identidad de un estudiante activo
     * y recibe un Bearer token de ese estudiante para Unity.
     */
    public function assumeStudent(Request $request, int $id, SessionEventBus $events)
    {
        $actor = $request->user();
        if (! in_array($actor->role, ['counselor', 'admin'], true)) {
            return response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Forbidden']], 403);
        }

        $student = User::query()->where('id', $id)->where('role', 'student')->first();
        if (! $student) {
            return response()->json(['error' => ['code' => 'NOT_FOUND', 'message' => 'Student not found']], 404);
        }

        $profile = StudentProfile::query()->where('user_id', $student->id)->first();
        if (! $profile || ! $profile->is_active) {
            return response()->json([
                'error' => ['code' => 'STUDENT_INACTIVE', 'message' => 'Student profile missing or inactive'],
            ], 422);
        }

        $this->console('[B] Assume student='.$student->id.' by '.$actor->username);

        $payload = $this->issueStudentTokenResponse($student, $profile, $events, 'assume_student');
        $data = $payload->getData(true);
        $data['assumed_by'] = [
            'id' => (string) $actor->id,
            'username' => $actor->username,
            'role' => $actor->role,
        ];

        return response()->json($data);
    }

    private function issueStudentTokenResponse(
        User $student,
        StudentProfile $profile,
        SessionEventBus $events,
        string $reason
    ) {
        $closed = $this->abortActiveSessionsForStudent($events, (int) $student->id, $reason);

        $plain = Str::random(48);
        ApiToken::query()->create([
            'id' => (string) Str::uuid(),
            'user_id' => $student->id,
            'token' => hash('sha256', $plain),
            'expires_at' => now()->addDays(7),
        ]);

        return response()->json([
            'token' => $plain,
            'token_type' => 'Bearer',
            'expires_at' => now()->addDays(7)->utc()->toIso8601String(),
            'user' => $this->userPayload($student->fresh()),
            'profile' => [
                'profile_id' => $profile->id,
                'user_id' => (string) $profile->user_id,
                'nombre_preferencia' => $profile->nombre_preferencia,
                'nombre' => $profile->nombre_preferencia,
                'grado' => $profile->grado,
                'sede' => $profile->sede,
                'jornada' => $profile->jornada,
                'documento_numero' => $profile->documento_numero,
            ],
            'closed_active_sessions' => $closed,
        ]);
    }

    private function validateStudentSchoolFields(Request $request, bool $requireSchoolExtras): array
    {
        // Contrato de A: nombre + numero_documento (aliases tolerados).
        if ($request->filled('numero_documento') && ! $request->filled('documento_numero')) {
            $request->merge(['documento_numero' => $request->input('numero_documento')]);
        }
        if ($request->filled('documento') && ! $request->filled('documento_numero')) {
            $request->merge(['documento_numero' => $request->input('documento')]);
        }
        if ($request->filled('name') && ! $request->filled('nombre')) {
            $request->merge(['nombre' => $request->input('name')]);
        }

        $rules = [
            'nombre' => 'required|string|max:120',
            'documento_numero' => 'required|string|max:64',
        ];

        if ($requireSchoolExtras) {
            // Registro: todos obligatorios.
            $rules['grado'] = 'required|string|max:64';
            $rules['sede'] = 'required|string|max:128';
            $rules['jornada'] = 'required|string|max:64';
        } else {
            // Ingreso: solo nombre + documento; el resto opcional.
            $rules['grado'] = 'sometimes|nullable|string|max:64';
            $rules['sede'] = 'sometimes|nullable|string|max:128';
            $rules['jornada'] = 'sometimes|nullable|string|max:64';
        }

        return $request->validate($rules);
    }

    private function normalizeDocumento(string $documentoNumero): string
    {
        $digits = preg_replace('/\D+/', '', $documentoNumero);

        return ($digits !== null && $digits !== '') ? $digits : trim($documentoNumero);
    }

    private function refreshProfileFromSchoolFields(StudentProfile $profile, array $data, string $documento): void
    {
        $nombre = trim($data['nombre']);
        $originalPref = (string) $profile->getOriginal('nombre_preferencia');

        $updates = [
            'nombre_preferencia' => $nombre,
            'documento_numero' => $documento,
        ];
        if (! empty($data['grado'])) {
            $updates['grado'] = trim((string) $data['grado']);
        }
        if (! empty($data['sede'])) {
            $updates['sede'] = trim((string) $data['sede']);
        }
        if (! empty($data['jornada'])) {
            $updates['jornada'] = trim((string) $data['jornada']);
        }

        $profile->fill($updates);
        if (trim((string) $profile->nombres) === ''
            || $this->normalizeKey((string) $profile->nombres) === $this->normalizeKey($originalPref)) {
            $profile->nombres = $nombre;
        }
        $profile->save();

        $student = $profile->user;
        $student->display_name = $nombre;
        $student->name = trim($profile->nombres.' '.$profile->apellidos) ?: $nombre;
        $student->save();
    }

    private function createStudentProfileFromSchoolFields(array $data, string $documento, ?int $createdBy): StudentProfile
    {
        $nombre = trim($data['nombre']);
        $grado = trim((string) ($data['grado'] ?? '')) ?: 'pendiente';
        $sede = trim((string) ($data['sede'] ?? '')) ?: 'pendiente';
        $jornada = trim((string) ($data['jornada'] ?? '')) ?: 'pendiente';
        $accessCode = $this->generateUniqueAccessCode();
        $emailLocal = 'stu.'.Str::lower(preg_replace('/[^A-Za-z0-9]/', '', $documento)).'.'.Str::lower(Str::random(4));
        $username = 'stu_'.Str::lower(preg_replace('/[^A-Za-z0-9]/', '', $documento));
        if (User::query()->where('username', $username)->exists()) {
            $username .= '_'.Str::lower(Str::random(4));
        }

        return DB::transaction(function () use ($documento, $createdBy, $nombre, $grado, $sede, $jornada, $accessCode, $emailLocal, $username) {
            $user = User::query()->create([
                'username' => $username,
                'name' => $nombre,
                'display_name' => $nombre,
                'email' => $emailLocal.'@empathia.local',
                'password' => null,
                'role' => 'student',
            ]);

            return StudentProfile::query()->create([
                'user_id' => $user->id,
                'nombres' => $nombre,
                'apellidos' => '-',
                'nombre_preferencia' => $nombre,
                'grado' => $grado,
                'edad' => 12,
                'sede' => $sede,
                'jornada' => $jornada,
                'documento_numero' => $documento,
                'acudiente_telefono' => 'pendiente',
                'acudiente_documento' => 'pendiente',
                'access_code' => $accessCode,
                'is_active' => true,
                'created_by' => $createdBy,
            ])->load('user');
        });
    }

    private function generateUniqueAccessCode(): string
    {
        do {
            $code = strtoupper(Str::random(8));
        } while (StudentProfile::query()->where('access_code', $code)->exists());

        return $code;
    }

    private function studentIdentityMatches(StudentProfile $profile, array $data): bool
    {
        // Documento ya aisló la fila. Nombre: igualdad normalizada (evita cruzar perfiles).
        $nombreIn = $this->normalizeKey($data['nombre']);
        if ($nombreIn === '') {
            return false;
        }

        $full = $this->normalizeKey(trim($profile->nombres.' '.$profile->apellidos));
        $pref = $this->normalizeKey($profile->nombre_preferencia);
        $nombresOnly = $this->normalizeKey((string) $profile->nombres);

        return $nombreIn === $pref
            || $nombreIn === $full
            || ($nombresOnly !== '' && $nombreIn === $nombresOnly);
    }

    private function normalizeKey(string $value): string
    {
        $v = mb_strtolower(trim($value), 'UTF-8');
        $map = [
            'á' => 'a', 'à' => 'a', 'ä' => 'a', 'â' => 'a',
            'é' => 'e', 'è' => 'e', 'ë' => 'e', 'ê' => 'e',
            'í' => 'i', 'ì' => 'i', 'ï' => 'i', 'î' => 'i',
            'ó' => 'o', 'ò' => 'o', 'ö' => 'o', 'ô' => 'o',
            'ú' => 'u', 'ù' => 'u', 'ü' => 'u', 'û' => 'u',
            'ñ' => 'n', '°' => '',
        ];
        $v = strtr($v, $map);

        return preg_replace('/\s+/', ' ', $v) ?? '';
    }

    private function normalizePreferredName(mixed $value): ?string
    {
        $name = trim((string) $value);
        if ($name === '') {
            return null;
        }

        // Quitar caracteres raros; C solo usa letras / ' / -
        $name = preg_replace("/[^\\p{L}'\\- ]+/u", '', $name) ?? '';
        $name = trim(preg_replace('/\s+/', ' ', $name) ?? '');
        if ($name === '') {
            return null;
        }

        $parts = preg_split('/\s+/', $name) ?: [];
        if (count($parts) > 2) {
            $name = $parts[0].' '.$parts[1];
        }

        if (mb_strlen($name) > 40) {
            $name = mb_substr($name, 0, 40);
        }

        return $name !== '' ? $name : null;
    }

    private function console(string $message): void
    {
        $line = $message.PHP_EOL;
        file_put_contents('php://stderr', $line);
        Log::info($message);
    }

    private function resolveSession(string $sessionId): AccompanimentSession
    {
        if ($sessionId === 'active') {
            $user = request()->user();
            $q = AccompanimentSession::query()
                ->where('status', 'active')
                ->orderByDesc('started_at');
            // Estudiante solo ve SU sesión activa (no la de otro perfil).
            if ($user && $user->role === 'student') {
                $q->where('student_user_id', $user->id);
            }
            $session = $q->first();
            if (! $session) {
                abort(response()->json([
                    'error' => ['code' => 'NOT_FOUND', 'message' => 'No active session'],
                ], 404));
            }

            return $session;
        }

        return AccompanimentSession::query()->findOrFail($sessionId);
    }

    private function abortActiveSessions(SessionEventBus $events, string $reason): int
    {
        $rows = AccompanimentSession::query()->where('status', 'active')->get();
        foreach ($rows as $session) {
            $session->status = 'closed';
            $session->ended_at = now();
            $session->save();
            $events->push($session, 'session.closed', ['reason' => $reason]);
            $events->push($session, 'session.state', ['state' => 'closed']);
        }

        return $rows->count();
    }

    private function abortActiveSessionsForStudent(SessionEventBus $events, int $studentUserId, string $reason): int
    {
        $rows = AccompanimentSession::query()
            ->where('status', 'active')
            ->where('student_user_id', $studentUserId)
            ->get();

        foreach ($rows as $session) {
            $session->status = 'closed';
            $session->ended_at = now();
            $session->save();
            $events->push($session, 'session.closed', ['reason' => $reason]);
            $events->push($session, 'session.state', ['state' => 'closed']);
        }

        return $rows->count();
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
