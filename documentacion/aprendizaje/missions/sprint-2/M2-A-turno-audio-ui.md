# M2-A — Turno con audio en el Avatar

## ID

`M2-A-turno-audio-ui`

## Título

Completar el primer turno: audio → events → texto + TTS

## Rol

A

## Objetivo

Después del login/sesión (Sprint 1), que Unity envíe un enunciado de audio al servidor, espere `turn.result` (poll de events) y muestre la respuesta (texto + reproducción de TTS).

## Tiempo estimado

120–180 minutos

## Competencias que desarrolla

- Multipart upload  
- Máquina de estados de UI  
- Consumo de events / URLs autenticadas  

## Conocimientos previos

- Sprint 1 A: login + token + sesión  
- README de B para curls de turno/events  
- Servidor B arriba en piloto  

## Entregables

1. Flujo UI: tras sesión → grabar o elegir WAV de prueba → enviar turno  
2. Poll a `GET .../sessions/{id}/events` hasta `turn.result` (o timeout con error claro)  
3. Mostrar `reply_text` en pantalla  
4. Reproducir TTS desde `tts.url` con Bearer  
5. Estados visibles: `listening` / `processing` / `speaking` (aunque sean labels)  
6. `cliente-unity/APRENDIZAJE.md` actualizado con el flujo del turno  

## Criterios de aceptación

- [ ] `POST .../turns` con `audio` + `client_turn_key` (UUID)  
- [ ] Se recibe `turn.accepted` o 202 y luego `turn.result` por events  
- [ ] `reply_text` visible  
- [ ] TTS se intenta reproducir (archivo wav)  
- [ ] `turn.error` muestra mensaje en español  
- [ ] No se llama a `:8100`  
- [ ] Checklist Sprint 2 A  

## Cómo validar

1. B (y C stub si aplica) arriba.  
2. Login → sesión → enviar WAV corto.  
3. Ver texto de respuesta + oír TTS.  
4. Demo ≤ 2 min al mentor.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| 202 pero nunca result | Poll `after` mal; revisar events con B |
| TTS 401 | Enviar Authorization en el download |
| SESSION no active | Recrear sesión / close previa |
| Audio vacío | Usar WAV de prueba de `herramientas` o silencio generado |

## Qué NO debo hacer

- Hablar con C directo  
- Exigir lip-sync perfecto (coordina con D un morph mínimo)  
- Cambiar contratos  

## Bonus

- Aplicar 1 morph/visema mientras `speaking` según packet o heurística de D  

## Referencias

- `contratos/api-rest/v1/openapi.yaml`  
- `contratos/websocket/v1/events.md`  
- Misión B Sprint 2  
- `documentacion/equipo/guia-rol-A-avatar.md`  

## Evidencia de cierre

Pantalla con reply + audio TTS en un turno real contra B.
