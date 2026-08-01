# PROJECT_MAP — Mapa de EmpathIA (1 página)

**Tiempo de lectura:** ≤ 5 minutos  
**Audiencia:** todo el equipo

---

## ¿Qué es?

EmpathIA es un **prototipo** de acompañamiento: un avatar 3D escucha al estudiante (voz), el sistema entiende y responde con empatía (texto + voz), y el avatar mueve labios/cara.

Todo corre en **un PC Windows** (piloto). Una sesión de avatar a la vez.

---

## Arquitectura (vista de cajas)

```text
                    ESTUDIANTE
                        │
                        ▼
            ┌───────────────────────┐
            │  A · Avatar (Unity)   │  Ve / oye / habla / anima
            │  cliente-unity/       │
            └───────────┬───────────┘
                        │ REST + eventos (poll/WS)
                        ▼
            ┌───────────────────────┐
            │  B · Servidor         │  Login, sesión, turnos,
            │  servidor/ (Laravel)  │  historial, riesgo, orquesta
            └───────────┬───────────┘
                        │ API interna InferTurn
                        ▼
            ┌───────────────────────┐
            │  C · Inteligencia     │  STT → análisis → LLM → TTS
            │  inteligencia/        │  memoria · señales de riesgo
            └───────────┬───────────┘
                        │ timing + emoción
                        ▼
            ┌───────────────────────┐
            │  D · Expresión        │  Define cómo se mueve la cara
            │  expresion/           │  ExpressionPacket (contrato)
            └───────────────────────┘
                        │
                        └──► A aplica el packet en Unity
```

**Contratos compartidos:** `contratos/` (nadie los cambia solo).

---

## Flujo de un turno (información)

```text
1. Micrófono (A) graba voz del estudiante
2. A envía audio → B (REST)
3. B pide inferencia → C (InferTurn)
4. C devuelve: texto, emoción, riesgo, respuesta, audio TTS, timing
5. B guarda lo institucional + arma eventos
6. A recibe resultado + ExpressionPacket (spec D)
7. A reproduce voz y mueve labios/cara
```

---

## Qué hace cada estudiante

| Rol | En una frase | Tecnología principal |
|-----|--------------|----------------------|
| **A** | La cara del sistema | Unity 6, C# |
| **B** | La recepción y el orden | Laravel, PHP, MySQL/SQLite, REST/WS |
| **C** | El que “piensa” y habla | Python, Whisper, Ollama, Kokoro (luego); stub hoy |
| **D** | El coach de expresión | Schemas JSON, visemas, fixtures |

---

## Cómo se relacionan (quién habla con quién)

| Desde | Hacia | ¿Permitido? |
|-------|-------|-------------|
| A | B | Sí — único front-door |
| B | C | Sí — orquestación |
| C/D | A | Solo vía datos que B transporta / contrato D |
| A | C | **No** |
| C | Base de datos de B | **No** |
| Cualquiera | `contratos/` | Solo con Contract Review |

---

## Dónde profundizar

- Roles en detalle técnico: `documentacion/equipo/guia-rol-*.md`  
- Overview rápido: [ROLE_OVERVIEW.md](./ROLE_OVERVIEW.md)  
- Contratos: `contratos/`  
- Manual del estudiante: [STUDENT_HANDBOOK.md](./STUDENT_HANDBOOK.md)
