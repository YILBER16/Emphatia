# Sprint 3 — Voz, nombre y boca (cerrar el circuito)

## De dónde venimos (ya logrado)

| Rol | Logrado (Sprint 0–2) |
|-----|----------------------|
| **A** | Unity: login adulto, lista de estudiantes, sesión, turno texto/audio, poll, `reply_text`, intento de TTS |
| **B** | API de turno, events, perfiles sin password (ADR-009), orquesta C, sirve TTS |
| **C** | Stub InferTurn, Gemini + prompts, nombre preferido sanitizado, Whisper opcional |
| **D** | Fixture ExpressionPacket de Fase 0 (**misiones D aún no hechas**) |

**Aún no existe de punta a punta:** el estudiante **oye** una respuesta de C y **ve** que el avatar habla (1 morph). El STT de laboratorio vive en Unity; el diseño pide que transcriba **C**.

## Objetivo del sprint

Cerrar el circuito del [PROJECT_MAP](../../PROJECT_MAP.md) en el PC piloto, todavía sin lip-sync fino ni WebSockets:

```text
Adulto elige estudiante → A envía audio o texto → B orquesta (INTEL_STUB=false)
→ C transcribe (si hay audio) + responde (Gemini o stub) + deja un WAV
→ B publica turn.result con preferred_name usado y expression
→ A muestra texto, reproduce TTS audible y aplica 1 morph en Speaking
```

## Resultado esperado por rol

| Rol | Hecho al cerrar Sprint 3 |
|-----|--------------------------|
| **A** | Camino principal: audio/texto a B (sin llamar a C). Oye TTS. Aplica 1 morph según packet o guía de D. STT local de Windows queda como fallback de lab, no como “done”. |
| **B** | Envía `preferred_name` del perfil a InferTurn. Demo con `INTEL_STUB=false`. TTS URL entrega el WAV de C (o stub con sonido, no solo silencio inaudible). |
| **C** | Con `audio.path`, transcribe (Whisper o fallback documentado). Devuelve un WAV que se oye. Plan STT→LLM→TTS escrito. Gemini/prompts se mantienen detrás del mismo contrato. |
| **D** | Pone al día Sprint 0–2 de expresión: APRENDIZAJE, tabla morphs con A, 1 morph mínimo en Speaking (o bloqueo técnico escrito). |

## Antes de abrir las misiones M3

En el piloto, **un turno Sprint 2** debe verse al menos una vez: login → assume → sesión → texto o WAV → `reply_text` en A.

Si eso no pasó todavía: hagan esa demo primero (30 min) y recién después las misiones de este sprint.

Si **no hay estudiante D**: A hace la mitad de [M3-D](./M3-D-tabla-y-morph.md) en pareja con el mentor (tabla + 1 morph). No se inventa otro schema.

## En casa (virtual)

Pasos para adelantar **sin laboratorio**: [sprint-3-tareas-casa.md](../../../equipo/sprint-3-tareas-casa.md)

## Misiones

- [M3-A](./M3-A-tts-y-morph.md) — TTS audible + 1 morph  
- [M3-B](./M3-B-nombre-y-c-real.md) — `preferred_name` + C en el circuito  
- [M3-C](./M3-C-stt-y-wav.md) — STT de audio + WAV de salida  
- [M3-D](./M3-D-tabla-y-morph.md) — tabla morphs + 1 expresión mínima  

## Definition of Done — Sprint 3

- [ ] En PC piloto, **B y C en la misma máquina** (paths de audio): un turno se oye  
- [ ] La respuesta usa (o descarta con regla) el nombre preferido del perfil  
- [ ] A aplica 1 morph en Speaking **o** D documenta bloqueo del avatar  
- [ ] Checklists A/B/C/D Sprint 3 en verde o parcial con bloqueo claro  
- [ ] `INTEL_STUB=false` se demostró al menos una vez  
- [ ] Nadie cambió `contratos/` sin Contract Review  
- [ ] Prueba de humo sigue OK  

## Fuera de alcance

- WebSockets nativos (sigue poll)  
- MySQL obligatorio  
- Lip-sync cinematográfico / visemas fotograma a fotograma  
- Memoria de C persistente  
- Ollama “porque sí” (Gemini ya cubre LLM si el contrato no cambia)  
- Alertas al orientador en vivo  

## Por qué este orden

Sprint 2 conectó el esqueleto. El cuello de botella ahora es **percibir** el prototipo: voz + boca + que C reciba el nombre que B ya guarda. STT en C evita que A sustituya a Inteligencia.
