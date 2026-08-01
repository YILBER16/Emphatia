<?php

namespace Database\Seeders;

use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        User::query()->updateOrCreate(
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

        User::query()->updateOrCreate(
            ['username' => 'admin1'],
            [
                'name' => 'Admin Uno',
                'display_name' => 'Admin Uno',
                'email' => 'admin1@empathia.local',
                'password' => Hash::make('password'),
                'role' => 'admin',
            ]
        );
    }
}
