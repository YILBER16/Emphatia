# Roles y límites (ownership)

## Mapa rápido

```text
Estudiante ──voz──► A (Unity)
                      │ REST + eventos (poll/WS)
                      ▼
                    B (Laravel) ──orquesta──► C (Intelligence)
                      │                            │
                      │ persiste turnos/riesgo     │ STT, emoción, riesgo,
                      │                            │ LLM, TTS, memoria
                      ▼                            ▼
                 MySQL/SQLite              store memoria C
                      │
                      └── ExpressionPacket (spec D) ──► A anima avatar
```

## Qué puede / no puede hacer cada uno

### A — Unity

| Puede | No puede |
|-------|----------|
| UI, micrófono, playback, avatar, lip-sync consumidor | Inferencia ML, SQL, catálogo de riesgo “de negocio” |
| Cliente REST/WS según contrato | Llamar a C o a Ollama |
| Implementar morphs según enums D | Inventar otro schema de expresión |

### B — Backend

| Puede | No puede |
|-------|----------|
| Auth, sesiones, turnos, APIs counselor, orquestación | Lógica de blendshapes Unity |
| Validar risk codes vs catálogo y persistir | Sustituir el LLM “porque es más fácil en PHP” |
| Feature flag `INTEL_STUB` | Exponer Ollama a la LAN |

### C — Intelligence

| Puede | No puede |
|-------|----------|
| Whisper, análisis, Ollama, Kokoro, memoria, InferTurn | Auth de usuarios institucionales |
| Emitir risk_signals con evidence | Insertar filas en tablas Laravel |
| Declarar `timing.quality` high/low | Conocer detalles de render 3D |

### D — Expression

| Puede | No puede |
|-------|----------|
| Schema ExpressionPacket, enums, fixtures, criterio lip-sync | Poseer el pipeline STT/LLM |
| Validar que A cumple el mapeo | Cambiar InferTurn sin acuerdo con C |
| Documentar degradación `timing_quality: low` | Guardar historial de riesgo |

## Contratos compartidos

Cualquier estudiante puede **proponer** un cambio en `contracts/`.  
Solo se mergea con review de **productor + consumidor**.

Ejemplos:

- Cambiar `turn.result` → review A + B  
- Cambiar InferTurn → review B + C  
- Cambiar ExpressionPacket → review A + C + D  
