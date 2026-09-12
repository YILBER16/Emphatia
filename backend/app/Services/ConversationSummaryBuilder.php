<?php

namespace App\Services;

use App\Models\Turn;

class ConversationSummaryBuilder
{
    public static function build(iterable $turns): string
    {
        $lines = [];
        foreach ($turns as $turn) {
            $lines[] = sprintf(
                'Turno %d [%s] | Usuario: %s | IA: %s',
                $turn->sequence_no,
                $turn->emotion_label ?: 'neutral',
                trim((string) $turn->transcript),
                trim((string) $turn->reply_text),
            );
        }

        return implode("\n", $lines);
    }
}