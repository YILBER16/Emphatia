# M2-B — Orquestación del turno para el Avatar

## ID

`M2-B-orquestacion-turno`

## Título

Dejar el turno (upload + events + TTS) estable y documentado

## Rol

B

## Objetivo

Que A pueda completar un turno solo con tu documentación y un servidor estable: multipart, events (`turn.result`), URL de TTS, errores claros. Stub de C detrás de `INTEL_STUB` o C real local.

## Tiempo estimado

90–150 minutos

## Competencias que desarrolla

- Orquestación de pipeline  
- Observabilidad básica (events/metrics)  
- Soporte a consumidor (A)  

## Conocimientos previos

- Sprint 1 B (README Para A, health, humo)  
- Código Fase 0 de turns/events  

## Entregables

1. Sección README **“Turno completo (para A)”**: upload, `client_turn_key`, poll events, TTS, close  
2. Verificación: prueba de humo OK **y** un turno manual con A o curl  
3. Documentar `INTEL_STUB=true/false` y cuándo necesita C en `:8100`  
4. Nota de fallos frecuentes: sesión activa, timeout, `turn.error`  
5. Confirmar que R1 (borrar input) no rompe el flujo de demo  

## Criterios de aceptación

- [ ] A logra un `turn.result` en el piloto con tu ayuda mínima  
- [ ] Events incluyen `turn.processing` y `turn.result` (o error tipado)  
- [ ] `GET .../turns/{id}/audio/tts` funciona con Bearer  
- [ ] Humo OK al final del día  
- [ ] Checklist Sprint 2 B  

## Cómo validar

1. Arrancar artisan.  
2. Seguir README “Turno completo” con curl o con A.  
3. Mostrar un `turn.result` en JSON de events.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Stub off y C caído | `INTEL_STUB=true` o levantar C |
| Sesión huérfana | Documentar close / abort |
| Path audio Windows | Usar `EMPATHIA_DATA_ROOT` correcto |

## Qué NO debo hacer

- Reescribir contratos  
- WebSockets obligatorios este sprint  
- Meter lógica de blendshapes  

## Bonus

- Log claro de `TurnMetrics` en APRENDIZAJE  
- Probar `INTEL_STUB=false` + C stub una vez  

## Referencias

- `contratos/api-rest/`, `contratos/websocket/`  
- `documentacion/manuales/arranque-parada.md`  
- `backend/README.md`  

## Evidencia de cierre

README turno + demo events/`turn.result` en vivo.
