<?php

namespace App\Console\Commands;

use App\Models\AccompanimentSession;
use Illuminate\Console\Command;

class CloseActiveSessions extends Command
{
    protected $signature = 'empathia:close-active-sessions';

    protected $description = 'Cierra todas las sesiones de acompañamiento activas (desbloquea a Rol A)';

    public function handle(): int
    {
        $count = AccompanimentSession::query()
            ->where('status', 'active')
            ->update([
                'status' => 'closed',
                'ended_at' => now(),
            ]);

        $this->info("Sesiones activas cerradas: {$count}");

        return self::SUCCESS;
    }
}
