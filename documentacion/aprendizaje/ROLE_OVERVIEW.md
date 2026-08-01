# ROLE_OVERVIEW — Vista rápida de roles (1 página)

Complementa (no reemplaza) `documentacion/equipo/roles.md` y `guia-rol-*.md`.

---

## Tabla maestra

| | **A Avatar** | **B Servidor** | **C Inteligencia** | **D Expresión** |
|--|--------------|----------------|--------------------|-----------------|
| **Hace** | UI, mic, playback, estados, aplica ExpressionPacket | Auth, sesión, turnos, persistencia, orquesta C, APIs orientador | STT, emoción, riesgo, LLM, TTS, memoria, InferTurn | Schema ExpressionPacket, enums, fixtures, criterio lip-sync |
| **No hace** | IA, SQL, riesgo de negocio | Blendshapes, LLM propio | Auth usuarios, escribir MySQL de B, Unity | Pipeline STT/LLM, historial de riesgo |
| **Se comunica con** | Solo **B** | **A** (cliente) y **C** (interno) | Solo **B** | Contrato hacia **A/C/B** (spec); pairing con A |
| **Modifica** | `cliente-unity/` | `servidor/` (+ runbooks/tools en coordinación) | `inteligencia/` | `expresion/`; propuestas en `contratos/expresion/` |
| **Nunca modifica** | `servidor/`, `inteligencia/`, `contratos/` sin review | Lógica Unity, motores ML de C | `servidor/` DB, `cliente-unity/` | Inventar InferTurn; código Unity sin acuerdo con A |
| **Consume** | REST/eventos de B; ExpressionPacket | InferTurn de C; catálogo riesgo | Audio path + ids desde B | Timing/emoción (desde C vía contrato); feedback de A |
| **Expone** | Experiencia al estudiante | API ` /api/v1 ` + eventos sesión | API interna `/internal/v1` | Spec + fixtures (no servidor HTTP propio en MVP) |

---

## Carpetas (recordatorio)

```text
cliente-unity/   → solo A
servidor/        → solo B
inteligencia/    → solo C
expresion/       → solo D
contratos/       → todos proponen; merge con review productor+consumidor
```

---

## APIs (nivel mapa)

| API | Dueño | Consumidor |
|-----|-------|------------|
| `http://127.0.0.1:8000/api/v1/*` | B | A (y orientador) |
| Eventos sesión (poll/WS) | B | A |
| `http://127.0.0.1:8100/internal/v1/*` | C | Solo B |
| ExpressionPacket (JSON en `turn.result`) | Spec D | A (vía B) |

Detalle de contratos: `contratos/api-rest/`, `contratos/websocket/`, `contratos/inteligencia/`, `contratos/expresion/`.

---

## Siguiente paso

1. Lee tu [misión Sprint 0](./missions/sprint-0/).  
2. Usa tu [checklist](./checklists/sprint-0/).  
3. Cuando necesites profundidad: `documentacion/equipo/guia-rol-X-*.md`.
