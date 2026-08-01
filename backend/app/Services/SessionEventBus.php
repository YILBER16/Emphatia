<?php

namespace App\Services;

use App\Models\AccompanimentSession;
use App\Models\SessionEvent;
use Illuminate\Support\Carbon;

class SessionEventBus
{
    public function push(AccompanimentSession $session, string $type, array $payload): SessionEvent
    {
        return SessionEvent::query()->create([
            'session_id' => $session->id,
            'type' => $type,
            'payload' => $payload,
            'ts' => Carbon::now('UTC'),
        ]);
    }

    public function envelope(SessionEvent $event): array
    {
        return [
            'v' => 1,
            'type' => $event->type,
            'ts' => $event->ts->utc()->toIso8601String(),
            'session_id' => $event->session_id,
            'payload' => $event->payload,
            'id' => $event->id,
        ];
    }
}
