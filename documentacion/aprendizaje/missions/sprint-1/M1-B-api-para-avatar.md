# M1-B — API lista para el Avatar

## ID

`M1-B-api-para-avatar`

## Título

Dejar el servidor documentado y usable por A

## Rol

B

## Objetivo

Que cualquier compañero A pueda, solo con tu README, arrancar el servidor, hacer login, crear sesión y entender qué pasa en un turno (events / `turn.result`) — sin adivinar.

## Tiempo estimado

60–120 minutos

## Competencias que desarrolla

- Documentación de API para consumidores  
- Operación local estable  
- Orquestación (visión A→B→C)  

## Conocimientos previos

- Sprint 0 B (health OK)  
- Usuarios seed conocidos  

## Entregables

1. Sección **“Para el compañero A (Unity)”** en `backend/README.md` (o `servidor/README.md`)  
2. Ejemplos: login, crear sesión, (opcional) events, cerrar sesión — en español  
3. Confirmación escrita: health OK + (ideal) prueba de humo OK  
4. Breve explicación en README: qué es un turno y por qué existe `INTEL_STUB`  

## Criterios de aceptación

- [ ] A puede seguir el README sin preguntarte cada curl  
- [ ] `GET /api/v1/health` documentado y verificado hoy  
- [ ] Login documentado con `estudiante1` / `password`  
- [ ] Crear sesión documentado con header Authorization  
- [ ] Sabes explicar `SESSION_ALREADY_ACTIVE`  
- [ ] Checklist Sprint 1 B en verde  

## Cómo validar el resultado

1. Mentor o A sigue solo el README en otro PC/terminal.  
2. Login devuelve token.  
3. (Ideal) `herramientas/prueba-humo-fase0.ps1` → `PHASE 0 SMOKE OK`.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| README solo técnico en inglés | Reescribir en español claro |
| Olvidar Bearer en ejemplos | Agregar header completo |
| Sesión activa colgada | Documentar `close` o abortar en DB/tinker |

## Qué NO debo hacer

- WebSockets nuevos  
- Cambiar contratos  
- Implementar features de riesgo nuevas  

## Bonus

- Sección “Problemas frecuentes” (puerto ocupado, CORS si aplica, sesión activa)  
- Curl de `GET .../events?after=0`  

## Referencias

- `contratos/api-rest/v1/openapi.yaml`  
- `contratos/websocket/v1/events.md` (poll interim)  
- `documentacion/manuales/arranque-parada.md`  
- `documentacion/equipo/clase-2.md`  

## Evidencia de cierre

README abierto + health/humo en vivo.
