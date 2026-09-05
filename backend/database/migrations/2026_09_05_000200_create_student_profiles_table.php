<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->string('password')->nullable()->change();
        });

        Schema::create('student_profiles', function (Blueprint $table) {
            $table->id();
            $table->foreignId('user_id')->unique()->constrained('users')->cascadeOnDelete();
            $table->string('nombres');
            $table->string('apellidos');
            $table->string('nombre_preferencia');
            $table->string('grado', 64);
            $table->unsignedTinyInteger('edad');
            $table->string('sede', 128);
            $table->string('jornada', 64);
            $table->string('documento_numero', 64);
            $table->string('acudiente_telefono', 64);
            $table->string('acudiente_documento', 64);
            $table->string('access_code', 32)->unique();
            $table->boolean('is_active')->default(true);
            $table->foreignId('created_by')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('student_profiles');

        Schema::table('users', function (Blueprint $table) {
            $table->string('password')->nullable(false)->change();
        });
    }
};
