<?php

namespace Database\Seeders;

use App\Models\StudentProfile;
use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        $student = User::query()->updateOrCreate(
            ['username' => 'estudiante1'],
            [
                'name' => 'Estudiante Uno',
                'display_name' => 'Estudiante Uno',
                'email' => 'estudiante1@empathia.local',
                'password' => Hash::make('password'),
                'role' => 'student',
            ]
        );

        User::query()->updateOrCreate(
            ['username' => 'orientador1'],
            [
                'name' => 'Orientador Uno',
                'display_name' => 'Orientador Uno',
                'email' => 'orientador1@empathia.local',
                'password' => Hash::make('password'),
                'role' => 'counselor',
            ]
        );

        $admin = User::query()->updateOrCreate(
            ['username' => 'admin1'],
            [
                'name' => 'Admin Uno',
                'display_name' => 'Admin Uno',
                'email' => 'admin1@empathia.local',
                'password' => Hash::make('password'),
                'role' => 'admin',
            ]
        );

        // Perfil demo (lab). access_code regenerable en Fase 2; fijo aquí para pruebas.
        StudentProfile::query()->updateOrCreate(
            ['user_id' => $student->id],
            [
                'nombres' => 'Estudiante',
                'apellidos' => 'Uno',
                'nombre_preferencia' => 'Estudiante Uno',
                'grado' => '8°',
                'edad' => 14,
                'sede' => 'Sede Lab',
                'jornada' => 'mañana',
                'documento_numero' => '1000000001',
                'acudiente_telefono' => '3000000001',
                'acudiente_documento' => '2000000001',
                'access_code' => 'DEMO01',
                'is_active' => true,
                'created_by' => $admin->id,
            ]
        );
    }
}
