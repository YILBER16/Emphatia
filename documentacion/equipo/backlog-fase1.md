# Backlog Fase 1 — tareas por rol

Usar como tablero inicial (GitHub Projects / Issues).  
Cada ítem: un assignee = owner del rol.  
Prioridad: **P0** bloquea integración · **P1** importante · **P2** mejora.

---

## Compartido (equipo)

| ID | Prioridad | Tarea |
|----|-----------|--------|
| T0.1 | P0 | Asignar nombres en tabla de roles (`KICKOFF.md`) |
| T0.2 | P0 | Remoto Git + branch protection en `main` |
| T0.3 | P0 | Cada rol confirma smoke Fase 0 OK |
| T0.4 | P1 | Calendario de uso del PC piloto |
| T0.5 | P1 | Integration Friday semanal agendado |

---

## A — Unity

| ID | Prioridad | Tarea | Depends |
|----|-----------|--------|---------|
| A1 | P0 | Crear proyecto Unity 6 en `client-unity/` | — |
| A2 | P0 | Cliente login + guardar token | B API |
| A3 | P0 | Crear/cerrar sesión + UI estados | A2 |
| A4 | P0 | Grabar WAV + POST turn multipart | A3 |
| A5 | P0 | Poll events + mostrar reply_text | A4 |
| A6 | P1 | Reproducir TTS autenticado | A5 |
| A7 | P1 | Aplicar ExpressionPacket (mínimo lips) | D1, A6 |
| A8 | P1 | UI de `turn.error` | A5 |
| A9 | P2 | Migrar poll → WS cuando B entregue F1.4 | B5 |

---

## B — Backend

| ID | Prioridad | Tarea | Depends |
|----|-----------|--------|---------|
| B1 | P0 | Doc corta “API para A” (ejemplos curl) en `backend/README` o `docs/` | — |
| B2 | P0 | Preparar MySQL + guía migración desde SQLite | ADR-007 |
| B3 | P0 | Probar `INTEL_STUB=false` contra stub C | C stub |
| B4 | P1 | Session stale → aborted + config timeout | A heartbeat luego |
| B5 | P1 | WebSocket v1 (mismo envelope) | A9 consume |
| B6 | P1 | Actualizar smoke si cambia algo breaking | — |
| B7 | P2 | Backup runbook MySQL | B2 |

---

## C — Intelligence

| ID | Prioridad | Tarea | Depends |
|----|-----------|--------|---------|
| C1 | P0 | Estructura carpetas + README de modelos ES | — |
| C2 | P0 | Test harness: WAV → JSON InferTurn (stub) | — |
| C3 | P1 | Spike Whisper.cpp español (medición calidad/latencia) | hardware |
| C4 | P1 | Spike Ollama español + borrador system prompt/guardrails | — |
| C5 | P1 | Spike Kokoro ES + qué timing puedes emitir | D |
| C6 | P2 | Diseño memoria (K + resumen) en papel/ADR | — |
| C7 | P0 | No romper stub hasta feature-flag de motor real | B3 |

---

## D — Expression

| ID | Prioridad | Tarea | Depends |
|----|-----------|--------|---------|
| D1 | P0 | Inventario morphs del avatar vs enums | A1 |
| D2 | P0 | Doc mapeo viseme/gesture → blendshapes | D1 |
| D3 | P1 | Fixture `timing_quality: high` sintético | — |
| D4 | P1 | Sesión de integración lips con A | A7 |
| D5 | P1 | Criterio aceptación lip-sync piloto (escrito) | D4 |
| D6 | P2 | Reglas emotion_drive → face default | — |

---

## Orden de desbloqueo (crítico)

```text
T0.* roles + smoke
  → A1–A5 y B1 en paralelo
  → D1–D2 con A1
  → C1–C2 en paralelo (no bloquea A)
  → A6–A7 + D4
  → B3 con C stub
  → B5 ↔ A9
  → C3–C5 spikes hacia Fase 2
```

---

## Qué cuenta como “empecé mi rol”

No “leí el README”. Sino:

- Branch creada, **y**  
- Al menos un PR o commit en tu carpeta hacia tu primera tarea P0, **y**  
- Smoke sigue pasando en `main`.
