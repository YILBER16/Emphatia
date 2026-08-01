<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class SessionEvent extends Model
{
    protected $fillable = [
        'session_id',
        'type',
        'payload',
        'ts',
    ];

    protected function casts(): array
    {
        return [
            'payload' => 'array',
            'ts' => 'datetime',
        ];
    }

    public function session(): BelongsTo
    {
        return $this->belongsTo(AccompanimentSession::class, 'session_id');
    }
}
