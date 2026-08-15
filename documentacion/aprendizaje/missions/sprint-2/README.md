# Sprint 2 — Primer turno de conversación (esqueleto vivo)

## De dónde venimos (ya logrado)

| Rol | Logrado (Sprint 0–1) |
|-----|----------------------|
| **A** | Proyecto Unity + avatars; cliente login/sesión (`EmpathiaApiClient`, etc.) |
| **B** | API health, humo, README para A, APRENDIZAJE |
| **C** | Stub InferTurn, `PRUEBA_STUB.md`, contrato explicado |
| **D** | Fixture ExpressionPacket + misión de tabla morphs (Sprint 1) |

**Aún no existe de punta a punta:** micrófono → turno → respuesta hablada → boca.

## Objetivo del sprint

Que el equipo complete el **primer turno mínimo del mapa EmpathIA** con stub de IA (sin Whisper/Ollama/Kokoro obligatorios):

```text
A graba/envía audio → B acepta turno → (stub C) → events turn.result
→ A muestra texto + reproduce TTS → D/A aplican expresión mínima o packet visible
```

## Resultado esperado por rol

| Rol | Hecho al cerrar Sprint 2 |
|-----|--------------------------|
| **A** | Tras sesión: subir audio de turno, poll events, mostrar `reply_text`, reproducir audio TTS |
| **B** | Turno multipart + events estables; documentado para A; humo OK; stub C opcional detrás de flag |
| **C** | Stub responde bien a paths reales de audio del piloto; InferTurn demo con B; plan escrito STT→LLM→TTS (sin exigir modelos instalados) |
| **D** | Tabla morphs cerrada con A; packet de ejemplo listo; guía de 1 morph mínimo en Speaking |

## Misiones

- [M2-A](./M2-A-turno-audio-ui.md) — turno en Unity  
- [M2-B](./M2-B-orquestacion-turno.md) — orquestación y docs del turno  
- [M2-C](./M2-C-turno-audio-inferturn.md) — stub + contrato + puente con B *(ampliada)*  
- [M2-D](./M2-D-expresion-en-turno.md) — expresión en el turno  

## Definition of Done — Sprint 2

- [ ] En PC piloto: login → sesión → **un turno** → `turn.result` visible en A (texto)  
- [ ] Audio TTS se puede oír (aunque sea silencio/stub)  
- [ ] Checklists A/B/C/D Sprint 2 en verde o parcial documentado  
- [ ] Prueba de humo sigue OK  
- [ ] Nadie cambió `contratos/` sin Contract Review  
- [ ] IA real **no** es requisito de cierre (solo plan / spike opcional en C)  

## Fuera de alcance

- Whisper / Ollama / Kokoro como “done”  
- WebSockets nativos (sigue poll)  
- Lip-sync cinematográfico  
- Multi-usuario / MySQL obligatorio  
- Alertas al orientador en vivo  

## Por qué este orden (arquitectura)

El [PROJECT_MAP](../../PROJECT_MAP.md) pide el flujo de turno completo. Login ya existe; el cuello de botella siguiente es **el turno**. Expresión e InferTurn avanzan en paralelo para no bloquearse.
