# Rol B — Backend Laravel (institucional)

**Owner de:** `backend/`, coordinación de `tools/` y runbooks de arranque  
**Publica:** REST + eventos (poll → WS) según `contracts/rest` y `contracts/ws`  
**Orquesta:** llamadas a C (`INTELLIGENCE_URL`)

---

## Tu misión

Ser el **único front-door** del cliente: identidad, sesión, turnos, persistencia, riesgo consultable, auditoría mínima. Orquestas; no infieres.

---

## Lecturas obligatorias

1. Kickoff + roles + colaboración  
2. `contracts/rest/v1/openapi.yaml`  
3. `contracts/ws/v1/events.md` + ADR-008  
4. `contracts/intelligence/v1/*.schema.json`  
5. `contracts/risk/v0/codes.json`  
6. `docs/adr/ADR-007-sqlite-phase0.md`  
7. Runbooks `start-stop.md`, `ports.md`

---

## Qué ya está hecho (Fase 0)

- Auth por bearer token, usuarios seed.  
- Sesión 1:1 (`SESSION_ALREADY_ACTIVE`).  
- POST turns + idempotencia `client_turn_key`.  
- Orquestador con `INTEL_STUB`.  
- Event bus + poll `/events`.  
- Persistencia turnos / risk / metrics (campos base).  
- GET TTS.  
- APIs counselor básicas (`/students`, `/risk-signals`, `/risk-catalog`).

**No asumas que Fase 0 = producción.** Falta MySQL, WS real, hardening, stale sessions, etc.

---

## Límites duros

- No pongas lógica de blendshapes en Laravel.  
- No dejes que A hable con C.  
- C propone riesgo; **tú validas catálogo y persistes**.  
- R1: borrar audio input tras STT OK (ya contemplado en orquestador).  
- Bind `127.0.0.1` en piloto.

---

## Entregables Fase 1 (tu DoD)

- [ ] Migrar (o preparar) **MySQL** según arquitectura; documentar en runbook.  
- [ ] Mantener `INTEL_STUB` y modo real (`INTEL_STUB=false` → C).  
- [ ] Sesiones stale → `aborted` (heartbeat / timeout).  
- [ ] WebSocket real alineado a envelopes v1 (reemplaza poll como canal primario; ADR-008).  
- [ ] Tests o checklist de idempotencia y “solo 1 sesión active”.  
- [ ] Runbook backup MySQL (+ recordar a C el store de memoria).  
- [ ] No romper `tools/phase0-smoke.ps1` (actualizarlo si cambia auth/paths).

---

## Orden de trabajo recomendado (B)

| Orden | Foco |
|-------|------|
| 1 | Estabilizar API Fase 0 + documentar endpoints para A |
| 2 | MySQL + seeds en Laragon |
| 3 | Integración real con C stub (`INTEL_STUB=false`) |
| 4 | Timeout / abort de sesión |
| 5 | WS (Reverb u opción acordada) + deprecar poll en docs |

---

## Cómo validar tú solo

```powershell
cd backend
php artisan serve --host=127.0.0.1 --port=8000
cd ..
.\tools\phase0-smoke.ps1
```

Con C arriba y `INTEL_STUB=false`: mismo smoke debe seguir verde.

---

## Dependencias

| Necesitas de | Qué |
|--------------|-----|
| A | Feedback de UX/errores de contrato |
| C | Health + InferTurn estable en `:8100` |
| D | Fixtures si ensamblas ExpressionPacket en B |

---

## Coordinación extra

Eres el **punto natural de integración** del PC (puertos, `.env`, arranque).  
No eres el jefe del equipo: no decides contratos solo.

Branch: `b/...`
