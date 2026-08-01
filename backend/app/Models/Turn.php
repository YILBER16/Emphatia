<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Turn extends Model
{
    public $incrementing = false;

    protected $keyType = 'string';

    protected $fillable = [
        'id',
        'session_id',
        'sequence_no',
        'client_turn_key',
        'status',
        'transcript',
        'reply_text',
        'emotion_label',
        'emotion_confidence',
        'tts_path',
        'expression_packet',
        'model_versions',
        'metrics',
        'risk_emitted',
    ];

    protected function casts(): array
    {
        return [
            'expression_packet' => 'array',
            'model_versions' => 'array',
            'metrics' => 'array',
            'risk_emitted' => 'boolean',
            'emotion_confidence' => 'float',
        ];
    }

    public function session(): BelongsTo
    {
        return $this->belongsTo(AccompanimentSession::class, 'session_id');
    }

    public function riskSignals(): HasMany
    {
        return $this->hasMany(RiskSignalRecord::class, 'turn_id');
    }
}
