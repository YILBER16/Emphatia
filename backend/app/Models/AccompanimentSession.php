<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;

class AccompanimentSession extends Model
{
    public $incrementing = false;

    protected $keyType = 'string';

    protected $fillable = [
        'id',
        'student_user_id',
        'status',
        'locale',
        'client',
        'ws_ticket',
        'started_at',
        'ended_at',
    ];

    protected function casts(): array
    {
        return [
            'started_at' => 'datetime',
            'ended_at' => 'datetime',
        ];
    }

    public function student(): BelongsTo
    {
        return $this->belongsTo(User::class, 'student_user_id');
    }

    public function turns(): HasMany
    {
        return $this->hasMany(Turn::class, 'session_id');
    }

    public function events(): HasMany
    {
        return $this->hasMany(SessionEvent::class, 'session_id');
    }
}
