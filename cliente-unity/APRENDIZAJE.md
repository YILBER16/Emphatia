# Aprendizaje — Rol A (Avatar / Unity)

**Misión:** M0-A · Entorno Unity  
**Carpeta de trabajo:** `cliente-unity/`

---

## Qué es mi rol

Soy el **Rol A — Avatar**. Soy la cara del sistema EmpathIA: el estudiante me ve, me habla y me oye.

En Unity me toca:

- Pantalla de login y sesión
- Captura de micrófono
- Estados de UI (`idle` / `listening` / `processing` / `speaking`)
- Reproducir el audio TTS de la respuesta
- Aplicar el `ExpressionPacket` (labios y cara) que define D

**No “pienso”.** No hago IA, STT, LLM ni TTS. Solo consumo lo que me entrega el **Servidor B** en `http://127.0.0.1:8000`.

---

## Estructura que entiendo

```text
EmpathIA/
├── cliente-unity/   ← YO (A). Aquí vive el proyecto Unity 6
├── servidor/        ← B. Login, sesión, turnos, eventos
├── inteligencia/    ← C. STT, LLM, TTS (interno; solo B lo llama)
├── expresion/       ← D. Spec y fixtures del ExpressionPacket
└── contratos/       ← Formas de hablar entre módulos (con review)
```

Proyecto Unity 6 creado dentro del monorepo:

```text
cliente-unity/
├── APRENDIZAJE.md
├── README.md
└── avatar/          ← proyecto Unity 6 (Assets, Packages, ProjectSettings…)
```

### Estado del entorno (Sprint 0)

- [x] Unity Hub instalado
- [x] Editor Unity 6 instalado
- [x] Proyecto Unity referenciado / creado bajo `cliente-unity/`

**Nota:** Proyecto `avatar` en `cliente-unity/avatar/`. La escena abre en Unity 6.

---

## Carpetas que no toco

| Carpeta | Por qué no la toco |
|---------|-------------------|
| `servidor/` | Es de B (auth, turnos, orquestación) |
| `inteligencia/` | Es de C; **nunca** llamo a `:8100`, Ollama ni Whisper |
| `expresion/` | Es de D; yo solo **consumo** el packet vía B |
| `contratos/` | Se proponen cambios con review; no invento JSON de expresión |

Si algo falla en la API → hablo con **B**.  
Si morphs / blendshapes → hablo con **D**.  
Nunca con C en directo.

### Por qué A no llama a `inteligencia/` directo

Porque la arquitectura manda un solo camino: **A → B → C**.  
B es quien autentica, guarda el historial institucional, orquesta InferTurn y me devuelve eventos + `ExpressionPacket`. Si yo saltara a C, rompería sesión, seguridad, riesgo y el contrato del equipo.

---

## Qué haré en Sprint 1

Enfoque (sin avatar final todavía):

1. Proyecto Unity 6 estable en `cliente-unity/`
2. Login de lab (`estudiante1` / `password`)
3. Crear / cerrar sesión de acompañamiento
4. Máquina de estados UI: `idle | listening | processing | speaking`
5. Mic → WAV → `POST` de turno a B
6. Poll de eventos hasta ver resultado (WS más adelante)
7. Mostrar texto de respuesta y manejar `turn.error` visible

Luego: TTS + avatar básico + lip-sync con D.

---

## Cómo valido solo (cuando B esté arriba)

```powershell
.\herramientas\prueba-humo-fase0.ps1
```

Secuencia mental: login → sesión → enviar WAV de prueba → ver `turn.result` → (después) TTS + un blendshape con fixture de D.

---

## Evidencia Sprint 0

- Sé la ruta: `cliente-unity/`
- Este archivo existe y puedo explicarlo en ~30 segundos
- No toqué `servidor/`, `inteligencia/` ni `contratos/`
