<?php

namespace Tests\Unit;

use App\Services\ConversationSummaryBuilder;
use stdClass;
use Tests\TestCase;

class ConversationSummaryBuilderTest extends TestCase
{
    public function test_summary_keeps_every_turn_without_a_limit(): void
    {
        $turns = [];
        for ($index = 1; $index <= 25; $index++) {
            $turn = new stdClass();
            $turn->sequence_no = $index;
            $turn->emotion_label = 'neutral';
            $turn->transcript = 'Mensaje '.$index;
            $turn->reply_text = 'Respuesta '.$index;
            $turns[] = $turn;
        }

        $summary = ConversationSummaryBuilder::build($turns);

        $this->assertStringContainsString('Mensaje 1', $summary);
        $this->assertStringContainsString('Mensaje 25', $summary);
        $this->assertSame(25, substr_count($summary, 'Turno '));
    }
}