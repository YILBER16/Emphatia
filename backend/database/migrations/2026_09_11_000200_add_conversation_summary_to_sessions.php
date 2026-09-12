<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('accompaniment_sessions', function (Blueprint $table): void {
            $table->text('conversation_summary')->nullable()->after('ended_at');
        });
    }

    public function down(): void
    {
        Schema::table('accompaniment_sessions', function (Blueprint $table): void {
            $table->dropColumn('conversation_summary');
        });
    }
};