<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->string('username')->nullable()->unique()->after('id');
            $table->string('display_name')->nullable()->after('name');
            $table->string('role', 32)->default('student')->after('display_name');
        });

        Schema::create('api_tokens', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->foreignId('user_id')->constrained()->cascadeOnDelete();
            $table->string('token', 64)->unique();
            $table->timestamp('expires_at')->nullable();
            $table->timestamps();
        });

        Schema::create('accompaniment_sessions', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->foreignId('student_user_id')->constrained('users')->cascadeOnDelete();
            $table->string('status', 32)->default('active');
            $table->string('locale', 8)->default('es');
            $table->string('client', 32)->default('unity');
            $table->string('ws_ticket', 64)->nullable();
            $table->timestamp('started_at');
            $table->timestamp('ended_at')->nullable();
            $table->timestamps();
        });

        Schema::create('turns', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->uuid('session_id');
            $table->foreign('session_id')->references('id')->on('accompaniment_sessions')->cascadeOnDelete();
            $table->unsignedInteger('sequence_no');
            $table->string('client_turn_key');
            $table->string('status', 32)->default('accepted');
            $table->text('transcript')->nullable();
            $table->text('reply_text')->nullable();
            $table->string('emotion_label', 32)->nullable();
            $table->float('emotion_confidence')->nullable();
            $table->string('tts_path')->nullable();
            $table->json('expression_packet')->nullable();
            $table->json('model_versions')->nullable();
            $table->json('metrics')->nullable();
            $table->boolean('risk_emitted')->default(false);
            $table->timestamps();

            $table->unique(['session_id', 'client_turn_key']);
            $table->unique(['session_id', 'sequence_no']);
        });

        Schema::create('risk_signal_records', function (Blueprint $table) {
            $table->uuid('id')->primary();
            $table->uuid('turn_id');
            $table->uuid('session_id');
            $table->foreignId('student_user_id')->constrained('users')->cascadeOnDelete();
            $table->string('code', 64);
            $table->string('severity', 16);
            $table->text('evidence');
            $table->float('confidence');
            $table->string('source', 64)->default('intelligence_v1');
            $table->timestamps();

            $table->foreign('turn_id')->references('id')->on('turns')->cascadeOnDelete();
            $table->foreign('session_id')->references('id')->on('accompaniment_sessions')->cascadeOnDelete();
        });

        Schema::create('session_events', function (Blueprint $table) {
            $table->id();
            $table->uuid('session_id');
            $table->string('type');
            $table->json('payload');
            $table->timestamp('ts');
            $table->timestamps();

            $table->foreign('session_id')->references('id')->on('accompaniment_sessions')->cascadeOnDelete();
            $table->index(['session_id', 'id']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('session_events');
        Schema::dropIfExists('risk_signal_records');
        Schema::dropIfExists('turns');
        Schema::dropIfExists('accompaniment_sessions');
        Schema::dropIfExists('api_tokens');

        Schema::table('users', function (Blueprint $table) {
            $table->dropColumn(['username', 'display_name', 'role']);
        });
    }
};
