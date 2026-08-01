<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class RiskSignalRecord extends Model
{
    public $incrementing = false;

    protected $keyType = 'string';

    protected $fillable = [
        'id',
        'turn_id',
        'session_id',
        'student_user_id',
        'code',
        'severity',
        'evidence',
        'confidence',
        'source',
    ];

    protected function casts(): array
    {
        return [
            'confidence' => 'float',
        ];
    }

    public function turn(): BelongsTo
    {
        return $this->belongsTo(Turn::class, 'turn_id');
    }
}
