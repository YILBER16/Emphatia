<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        // Si hay duplicados de documento, desactiva los más viejos para poder crear el unique.
        $dupes = DB::table('student_profiles')
            ->select('documento_numero', DB::raw('COUNT(*) as c'))
            ->groupBy('documento_numero')
            ->havingRaw('COUNT(*) > 1')
            ->pluck('documento_numero');

        foreach ($dupes as $doc) {
            $ids = DB::table('student_profiles')
                ->where('documento_numero', $doc)
                ->orderByDesc('id')
                ->pluck('id');
            $keep = $ids->shift();
            if ($ids->isNotEmpty()) {
                DB::table('student_profiles')
                    ->whereIn('id', $ids->all())
                    ->update([
                        'is_active' => false,
                        'documento_numero' => $doc.'_dup_'.$keep.'_'.uniqid(),
                    ]);
            }
        }

        Schema::table('student_profiles', function (Blueprint $table) {
            $table->unique('documento_numero');
        });
    }

    public function down(): void
    {
        Schema::table('student_profiles', function (Blueprint $table) {
            $table->dropUnique(['documento_numero']);
        });
    }
};
