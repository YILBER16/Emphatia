<?php

namespace App\Http\Controllers\Api\V1;

use App\Http\Controllers\Controller;
use App\Models\StudentProfile;
use App\Models\User;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Str;
use Illuminate\Validation\Rule;

class AdminStudentController extends Controller
{
    public function index(Request $request)
    {
        $this->assertAdmin($request->user());

        $query = StudentProfile::query()->with(['user', 'creator'])->orderByDesc('id');

        if ($request->boolean('active_only')) {
            $query->where('is_active', true);
        }

        $rows = $query->limit(200)->get()->map(fn (StudentProfile $p) => $this->profilePayload($p, includeAccessCode: false));

        return response()->json(['data' => $rows]);
    }

    public function show(Request $request, int $id)
    {
        $this->assertAdmin($request->user());

        $profile = StudentProfile::query()->with(['user', 'creator'])->find($id);
        if (! $profile) {
            return response()->json(['error' => ['code' => 'NOT_FOUND', 'message' => 'Student profile not found']], 404);
        }

        return response()->json(['data' => $this->profilePayload($profile, includeAccessCode: false)]);
    }

    public function store(Request $request)
    {
        $admin = $request->user();
        $this->assertAdmin($admin);

        $data = $request->validate([
            'nombres' => 'required|string|max:120',
            'apellidos' => 'required|string|max:120',
            'nombre_preferencia' => 'required|string|max:120',
            'grado' => 'required|string|max:64',
            'edad' => 'required|integer|min:5|max:25',
            'sede' => 'required|string|max:128',
            'jornada' => 'required|string|max:64',
            'documento_numero' => 'required|string|max:64',
            'acudiente_telefono' => 'required|string|max:64',
            'acudiente_documento' => 'required|string|max:64',
        ]);

        $existsDoc = StudentProfile::query()->where('documento_numero', $data['documento_numero'])->exists();
        if ($existsDoc) {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'documento_numero already registered'],
            ], 422);
        }

        $accessCode = $this->generateUniqueAccessCode();
        $displayName = trim($data['nombre_preferencia']);
        $fullName = trim($data['nombres'].' '.$data['apellidos']);
        $emailLocal = 'stu.'.Str::lower(preg_replace('/[^A-Za-z0-9]/', '', $data['documento_numero'])).'.'.Str::lower(Str::random(4));
        $username = 'stu_'.Str::lower(preg_replace('/[^A-Za-z0-9]/', '', $data['documento_numero']));
        if (User::query()->where('username', $username)->exists()) {
            $username .= '_'.Str::lower(Str::random(4));
        }

        $profile = DB::transaction(function () use ($data, $admin, $accessCode, $displayName, $fullName, $emailLocal, $username) {
            $user = User::query()->create([
                'username' => $username,
                'name' => $fullName,
                'display_name' => $displayName,
                'email' => $emailLocal.'@empathia.local',
                'password' => null,
                'role' => 'student',
            ]);

            return StudentProfile::query()->create([
                'user_id' => $user->id,
                'nombres' => $data['nombres'],
                'apellidos' => $data['apellidos'],
                'nombre_preferencia' => $data['nombre_preferencia'],
                'grado' => $data['grado'],
                'edad' => $data['edad'],
                'sede' => $data['sede'],
                'jornada' => $data['jornada'],
                'documento_numero' => $data['documento_numero'],
                'acudiente_telefono' => $data['acudiente_telefono'],
                'acudiente_documento' => $data['acudiente_documento'],
                'access_code' => $accessCode,
                'is_active' => true,
                'created_by' => $admin->id,
            ]);
        });

        $profile->load(['user', 'creator']);

        return response()->json([
            'data' => $this->profilePayload($profile, includeAccessCode: true),
            'message' => 'Student profile created. Save access_code now; it will not be shown again on GET.',
        ], 201);
    }

    public function update(Request $request, int $id)
    {
        $this->assertAdmin($request->user());

        $profile = StudentProfile::query()->with(['user', 'creator'])->find($id);
        if (! $profile) {
            return response()->json(['error' => ['code' => 'NOT_FOUND', 'message' => 'Student profile not found']], 404);
        }

        $data = $request->validate([
            'nombres' => 'sometimes|string|max:120',
            'apellidos' => 'sometimes|string|max:120',
            'nombre_preferencia' => 'sometimes|string|max:120',
            'grado' => 'sometimes|string|max:64',
            'edad' => 'sometimes|integer|min:5|max:25',
            'sede' => 'sometimes|string|max:128',
            'jornada' => 'sometimes|string|max:64',
            'documento_numero' => [
                'sometimes',
                'string',
                'max:64',
                Rule::unique('student_profiles', 'documento_numero')->ignore($profile->id),
            ],
            'acudiente_telefono' => 'sometimes|string|max:64',
            'acudiente_documento' => 'sometimes|string|max:64',
            'is_active' => 'sometimes|boolean',
        ]);

        if ($data === []) {
            return response()->json([
                'error' => ['code' => 'VALIDATION_ERROR', 'message' => 'No fields to update'],
            ], 422);
        }

        DB::transaction(function () use ($profile, $data) {
            $profile->fill($data)->save();

            $userUpdates = [];
            if (isset($data['nombre_preferencia'])) {
                $userUpdates['display_name'] = trim($data['nombre_preferencia']);
            }
            if (isset($data['nombres']) || isset($data['apellidos'])) {
                $nombres = $data['nombres'] ?? $profile->nombres;
                $apellidos = $data['apellidos'] ?? $profile->apellidos;
                $userUpdates['name'] = trim($nombres.' '.$apellidos);
            }
            if ($userUpdates !== []) {
                $profile->user->fill($userUpdates)->save();
            }
        });

        $profile->refresh()->load(['user', 'creator']);

        return response()->json(['data' => $this->profilePayload($profile, includeAccessCode: false)]);
    }

    public function regenerateCode(Request $request, int $id)
    {
        $this->assertAdmin($request->user());

        $profile = StudentProfile::query()->with(['user', 'creator'])->find($id);
        if (! $profile) {
            return response()->json(['error' => ['code' => 'NOT_FOUND', 'message' => 'Student profile not found']], 404);
        }

        $profile->access_code = $this->generateUniqueAccessCode();
        $profile->save();

        return response()->json([
            'data' => $this->profilePayload($profile, includeAccessCode: true),
            'message' => 'access_code regenerated. Save it now; it will not be shown again on GET.',
        ]);
    }

    public function deactivate(Request $request, int $id)
    {
        $this->assertAdmin($request->user());

        $profile = StudentProfile::query()->with(['user', 'creator'])->find($id);
        if (! $profile) {
            return response()->json(['error' => ['code' => 'NOT_FOUND', 'message' => 'Student profile not found']], 404);
        }

        $profile->is_active = false;
        $profile->save();

        return response()->json(['data' => $this->profilePayload($profile, includeAccessCode: false)]);
    }

    private function assertAdmin(User $user): void
    {
        if ($user->role !== 'admin') {
            abort(response()->json(['error' => ['code' => 'FORBIDDEN', 'message' => 'Only admin can manage student profiles']], 403));
        }
    }

    private function generateUniqueAccessCode(): string
    {
        do {
            $code = strtoupper(Str::random(8));
        } while (StudentProfile::query()->where('access_code', $code)->exists());

        return $code;
    }

    private function profilePayload(StudentProfile $profile, bool $includeAccessCode): array
    {
        $payload = [
            'id' => $profile->id,
            'user_id' => (string) $profile->user_id,
            'nombres' => $profile->nombres,
            'apellidos' => $profile->apellidos,
            'nombre_preferencia' => $profile->nombre_preferencia,
            'display_name' => $profile->resolvedDisplayName(),
            'grado' => $profile->grado,
            'edad' => $profile->edad,
            'sede' => $profile->sede,
            'jornada' => $profile->jornada,
            'documento_numero' => $profile->documento_numero,
            'acudiente_telefono' => $profile->acudiente_telefono,
            'acudiente_documento' => $profile->acudiente_documento,
            'is_active' => (bool) $profile->is_active,
            'created_by' => $profile->created_by ? (string) $profile->created_by : null,
            'username' => $profile->user?->username,
            'created_at' => optional($profile->created_at)?->utc()->toIso8601String(),
            'updated_at' => optional($profile->updated_at)?->utc()->toIso8601String(),
        ];

        if ($includeAccessCode) {
            $payload['access_code'] = $profile->access_code;
        }

        return $payload;
    }
}
