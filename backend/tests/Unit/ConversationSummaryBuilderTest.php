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

    public function test_summary_is_scoped_to_the_registered_student_session(): void
    {
        $session = new \stdClass();
        $session->id = 'session-registered-student';
        $session->student_user_id = 42;
        $session->conversation_summary = ConversationSummaryBuilder::build([
            (object) [
                'sequence_no' => 1,
                'emotion_label' => 'anxiety',
                'transcript' => 'Me preocupa el examen.',
                'reply_text' => 'Podemos revisar una cosa a la vez.',
            ],
            (object) [
                'sequence_no' => 2,
                'emotion_label' => 'sadness',
                'transcript' => 'Tambien temo decepcionar a mi familia.',
                'reply_text' => 'Entiendo por que eso te pesa.',
            ],
        ]);

        $this->assertSame('session-registered-student', $session->id);
        $this->assertSame(42, $session->student_user_id);
        $this->assertStringContainsString('Turno 1', $session->conversation_summary);
        $this->assertStringContainsString('Turno 2', $session->conversation_summary);
    }
}