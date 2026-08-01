# Kickoff — Día 0 del equipo EmpathIA

## 1. Asignación de roles (obligatoria)

Antes de tocar código, el equipo confirma por escrito (chat/issue):

| Rol | Módulo | Carpeta principal | Nombre del estudiante |
|-----|--------|-------------------|------------------------|
| **A** | Cliente Unity / experiencia | `client-unity/` | _asignar_ |
| **B** | Backend institucional | `backend/` | _asignar_ |
| **C** | Pipeline de inteligencia | `intelligence/` | _asignar_ |
| **D** | Expresión y sincronización | `expression/` + `contracts/expression/` | _asignar_ |

Un solo owner por carpeta. Nadie mergea en carpeta ajena sin review del owner.

Actualizar `CODEOWNERS` con los usuarios reales de GitHub cuando exista el remoto.

---

## 2. Qué ya existe (no reinventar)

La Fase 0 entregó:

1. **Contratos v1** en `contracts/` — fuente de verdad.
2. **Backend Laravel** con auth, sesión, turnos, eventos (poll), riesgo, `INTEL_STUB`.
3. **Stub Intelligence** (`intelligence/stub_server.py`).
4. **Fixture ExpressionPacket** (`expression/fixtures/`).
5. **Smoke E2E** sin Unity: `tools/phase0-smoke.ps1`.

**Regla:** si tu feature contradice un contrato, **cambias el contrato con PR y acuerdo**, no “lo arreglo en mi módulo”.

---

## 3. Checklist individual (primera sesión, ~2 h)

Cada estudiante hace esto **solo**:

1. Clonar / abrir el monorepo en Cursor.
2. Leer: este kickoff + su guía de rol (`A/B/C/D-*.md`) + `COLLABORATION.md`.
3. Leer su sección de contratos (ver tabla en la guía de rol).
4. Arrancar el stack Fase 0 (runbook) y pasar el smoke:
   ```powershell
   cd C:\laragon\www\Emphatia
   # Terminal 1: intelligence stub
   # Terminal 2: php artisan serve
   .\tools\phase0-smoke.ps1
   ```
5. Abrir un issue o nota: “Rol X listo — smoke OK” o listar bloqueos.
6. Tomar de `PHASE1-BACKLOG.md` **su** primera tarea y crear branch `a/...` | `b/...` | `c/...` | `d/...`.

---

## 4. Checklist grupal (misma semana)

| # | Acción | Quién lidera |
|---|--------|--------------|
| 1 | Confirmar roles y horarios de uso del PC piloto | Todos |
| 2 | Crear remoto Git + proteger `main` (PRs obligatorios) | B o mentor |
| 3 | Reemplazar placeholders en `CODEOWNERS` | Todos |
| 4 | Acordar día de **Integration Friday** (1 turno E2E en el PC) | Todos |
| 5 | Congelar: no cambiar `contracts/v1` sin Contract Review | Todos |
| 6 | Orientador (si hay): revisar catálogo riesgo v0 como “provisional lab” | Mentor / dominio |

---

## 5. Principios que no se negocian

1. **A presenta, B orquesta, C razona, D especifica expresión.**
2. Unity **nunca** llama a Ollama/Whisper directo.
3. Intelligence **nunca** escribe el historial institucional en MySQL de B.
4. Una sola sesión activa en el PC piloto.
5. Español en STT/LLM/TTS.
6. Riesgo = señal con evidencia; no diagnóstico clínico.
7. Trabajo en paralelo = stubs + contratos, no esperar a que “el otro termine todo”.

---

## 6. Dónde preguntar qué

| Tipo de duda | Canal correcto |
|--------------|----------------|
| “¿Qué significa este evento WS?” | `contracts/ws/v1/events.md` → luego Contract Review |
| “¿Mi módulo puede hacer X?” | Guía de rol + `ROLES.md` |
| “El smoke falla” | `docs/runbooks/` + owner B/C |
| “Cambio de arquitectura” | ADR nuevo en `docs/adr/` + aprobación del equipo |

---

## 7. Definition of Ready para empezar Fase 1

- [ ] 4 roles asignados  
- [ ] Smoke Fase 0 OK en el PC del equipo  
- [ ] Cada uno leyó su guía  
- [ ] Branches por módulo acordados  
- [ ] Primera tarea de backlog reclamada por cada rol  
