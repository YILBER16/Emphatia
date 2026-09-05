# APRENDIZAJE — Rol B (Servidor)

## ¿Qué es el rol B?

Soy el **organizador del sistema**. Todo lo que hace Unity (rol A) pasa por mí antes de llegar a la Inteligencia (rol C). No animo el avatar ni infiero emociones: **orquesto, guardo y devuelvo resultados**.

## Flujo A → B → C (en 1 minuto)

```text
Estudiante habla en Unity (A)
      ↓
A envía audio al servidor (B) — POST /turns
      ↓
B guarda el turno, llama a C (o usa INTEL_STUB si C no está)
      ↓
C devuelve transcripción, respuesta, riesgo y expresión
      ↓
B valida, persiste y publica eventos (turn.result)
      ↓
A consulta /events y anima la cara con los datos de D
```

**Regla de oro:** Unity **nunca** habla directo con C. Siempre pasa por B.

## ¿Qué orquesta B?

| Responsabilidad | Endpoint / servicio |
|-----------------|---------------------|
| Login y tokens | `POST /auth/login` |
| Sesión de acompañamiento | `POST /accompaniment/sessions` |
| Subir turno (audio) | `POST .../turns` |
| Eventos en tiempo real (poll) | `GET .../events` |
| Audio TTS de respuesta | `GET .../turns/{id}/audio/tts` |
| Cerrar sesión | `POST .../close` |
| Health del sistema | `GET /health` |

## ¿Qué NO hace B?

- No calcula blendshapes ni visemas (eso es D + A).
- No corre modelos de IA dentro de Laravel (eso es C).
- No decide contratos solo; los compartidos viven en `contratos/`.

## ¿Qué es `INTEL_STUB`?

Variable en `.env`. Cuando es `true` (por defecto en Fase 0):

- B **no necesita** que C esté corriendo.
- El orquestador simula la respuesta de inteligencia localmente.
- El health reporta `intel_stub: true` y status `ok`.

Cuando es `false`, B llama a `http://127.0.0.1:8100` (servidor de C).

## Usuarios de prueba

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| estudiante1 | password | estudiante (solo demos de desarrollo) |
| orientador1 | password | orientador |
| admin1 | password | admin |

## Perfiles de estudiante (acuerdo Fase 0)

**Objetivo:** el admin crea perfiles; el estudiante **no** usa usuario/contraseña. En Unity, un adulto hace login y elige al estudiante de una lista.

Decisiones cerradas: [ADR-009](../documentacion/decisiones/ADR-009-perfiles-estudiante-sin-password.md) · checklist [Fase 0](../documentacion/aprendizaje/checklists/fase-perfiles/checklist-fase-0-acuerdos.md).

| Quién | Puede |
|-------|--------|
| Solo **admin** | Crear, editar, regenerar `access_code`, desactivar |
| **admin** y **counselor** | Listar y elegir estudiante (assume) para operar |
| Estudiante | Sin password; historial vía `student_user_id` |

Campos del perfil: nombres, apellidos, nombre de preferencia, grado, edad, sede, jornada, documento, teléfono y documento del acudiente, `access_code` regenerable, `is_active`.

**Fase 1 hecha:** tabla `student_profiles`, modelo + relación `User::studentProfile()`, seed demo (`access_code=DEMO01`).  
**Fase 2 hecha:** API admin `/api/v1/admin/students` (CRUD, regenerar código, desactivar). Solo rol `admin`.  
**Fase 3 hecha:** `GET /students` (lista activa) + `POST /students/{id}/assume` (token estudiante para Unity).  
**Fase 4 hecha:** UI Unity — login adulto → lista → assume → sesión (demo `estudiante1` sigue disponible).  
**Próximo:** Fase 5 — demo integrado y docs finales.

## Cómo arrancar

```powershell
cd C:\Emphatia\backend
php artisan serve --host=127.0.0.1 --port=8000
```

Probar: `http://127.0.0.1:8000/api/v1/health`

Prueba de humo completa:

```powershell
cd C:\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

Debe terminar con: `PHASE 0 SMOKE OK`

## Mi carpeta

- Trabajo en: `backend/` (también puede llamarse `servidor/` si creas el enlace).
- No toco: `cliente-unity/`, `inteligencia/`, `expresion/` (salvo integración acordada).
