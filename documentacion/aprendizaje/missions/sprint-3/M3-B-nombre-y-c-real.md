# M3-B — Nombre preferido y C en el circuito

## ID

`M3-B-nombre-y-c-real`

## Título

Pasar el nombre del perfil a C y demostrar el turno con INTEL_STUB apagado

## Rol

B

## Objetivo

Que InferTurn reciba el `preferred_name` del `student_profile` (ADR-009) y que un turno del piloto pase **de verdad** por C (`INTEL_STUB=false`), sirviendo un WAV que A pueda oír.

## Tiempo estimado

90–150 minutos

## Competencias que desarrolla

- Orquestación A→B→C  
- Datos de perfil sin filtrar PII de más  
- Operación del flag `INTEL_STUB`  

## Conocimientos previos

- Sprint 2 B: turno, events, TTS URL  
- ADR-009 (campo `nombre_preferencia`)  
- C ya sanitiza `preferred_name` en InferTurn  

## Entregables

1. En `callIntelligence` / `callIntelligenceText`, enviar `preferred_name` (solo el nombre, no documento ni teléfono)  
2. Sección README: cómo poner `INTEL_STUB=false` y apuntar a C en `:8100`  
3. Un turno curl o Unity con stub **off** y C arriba → `turn.result`  
4. Confirmar que `GET .../turns/{id}/audio/tts` entrega el `tts.path` que devolvió C  
5. Checklist Sprint 3 B  

## Criterios de aceptación

- [ ] C recibe `preferred_name` en turnos de texto y de audio  
- [ ] Si el perfil no tiene nombre, omites el campo (no inventas apodos)  
- [ ] Demo `INTEL_STUB=false` al menos una vez (log B→C visible)  
- [ ] Si C cae, el comportamiento queda documentado (error o fallback; sin mentir en README)  
- [ ] No envías documento, teléfono ni nombre completo a C  
- [ ] Humo OK  
- [ ] Checklist Sprint 3 B  

## Cómo validar

1. Perfil con `nombre_preferencia` (ej. Anita).  
2. C arriba. `.env`: `INTEL_STUB=false`.  
3. Turno texto: la reply debe poder usar el nombre (si Gemini está on) o el stub de C.  
4. Descargar TTS y confirmar que no está vacío.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Stub on “porque es más fácil” | Este sprint exige una demo con C real |
| Mandar todo el perfil a Gemini | Solo `preferred_name` |
| Path de audio en otro PC | B y C en el mismo piloto |
| URL TTS con host 127.0.0.1 hacia Unity en LAN | Mismo criterio Sprint 2: host alcanzable por A |

## Qué NO debo hacer

- WebSockets  
- Migración MySQL  
- Lógica de blendshapes  
- Cambiar el schema InferTurn (el campo `preferred_name` ya existe)  

## Bonus

- Log `[B→C] preferred_name=...` (sin otros datos del menor)  
- Documentar timeout si C tarda en Whisper  

## Referencias

- ADR-009  
- `backend/app/Services/TurnOrchestrator.php`  
- `contratos/inteligencia/v1/infer-turn.request.schema.json`  
- `documentacion/equipo/guia-rol-B-servidor.md`  

## Evidencia de cierre

Log B→C con nombre + `turn.result` + TTS descargable, con stub apagado.
