<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class StudentProfile extends Model
{
    protected $fillable = [
        'user_id',
        'nombres',
        'apellidos',
        'nombre_preferencia',
        'grado',
        'edad',
        'sede',
        'jornada',
        'documento_numero',
        'acudiente_telefono',
        'acudiente_documento',
        'access_code',
        'is_active',
        'created_by',
    ];

    protected function casts(): array
    {
        return [
            'edad' => 'integer',
            'is_active' => 'boolean',
        ];
    }

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    public function creator(): BelongsTo
    {
        return $this->belongsTo(User::class, 'created_by');
    }

    public function resolvedDisplayName(): string
    {
        $preferencia = trim((string) $this->nombre_preferencia);
        if ($preferencia !== '') {
            return $preferencia;
        }

        return trim($this->nombres.' '.$this->apellidos);
    }
}
