# Servidor (módulo B) — Laravel

API central de EmpathIA. Unity (A) habla **solo** con este servidor; el servidor llama a Inteligencia (C).

## Arrancar

```powershell
cd C:\Emphatia\backend
php artisan serve --host=127.0.0.1 --port=8000
```

Usuarios de prueba: `estudiante1` / `orientador1` / `admin1` — contraseña `password`.

Manual: `../documentacion/manuales/arranque-parada.md`.

**Nota:** en la documentación del equipo la carpeta se llama `servidor/`. En GitHub es `backend/` (misma app). Puedes crear el enlace:

```powershell
cd C:\Emphatia
cmd /c "mklink /J servidor backend"
```

---

## Para el compañero A (Unity)

Esta sección es para que puedas conectar Unity **sin preguntarle a B cada paso**.

### Base URL

`http://127.0.0.1:8000/api/v1`

### Flujo resumido

```text
1. Login          → obtienes token
2. Crear sesión   → obtienes session.id
3. (Más adelante) Subir turno con audio
4. Consultar events → esperas turn.result
5. Cerrar sesión  → cuando el estudiante termina
```

### ¿Qué es un turno?

Un **turno** es una ronda de conversación: el estudiante habla (audio) y el sistema responde.

1. A envía audio → `POST .../turns`
2. B acepta el turno (`turn.accepted`)
3. B orquesta: STT → IA (C) → TTS + expresión
4. B publica `turn.result` en `/events` con:
   - `transcript` — lo que dijo el estudiante
   - `reply_text` — respuesta del avatar
   - `expression` — datos para animar la cara (rol D)
   - `tts.url` — audio de la respuesta

Hoy A solo necesita login + sesión. El turno completo lo conectarás en Sprint 1 con B.

### ¿Qué es `INTEL_STUB`?

Variable en `.env` del servidor. Cuando es `true` (por defecto):

- B **no necesita** que C (Inteligencia) esté corriendo.
- El orquestador simula la respuesta de la IA localmente.
- El health devuelve `status: ok` y `intel_stub: true`.

Cuando es `false`, B llama a `http://127.0.0.1:8100` (servidor de C).

**Para desarrollar solo con A y B:** deja `INTEL_STUB=true`.

### Health (sin login)

Verifica que el servidor está vivo antes de conectar Unity:

```powershell
curl.exe -s "http://127.0.0.1:8000/api/v1/health"
```

Respuesta esperada:

```json
{"status":"ok","checks":{"db":true,"intelligence":true,"intel_stub":true}}
```

`status` puede ser `ok` o `degraded` (si C no está pero `INTEL_STUB=true`, sigue siendo usable).

---

### 1) Login — obtener token

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"estudiante1\",\"password\":\"password\"}"
```

Respuesta (ejemplo):

```json
{"token":"abc123...","token_type":"Bearer","user":{"username":"estudiante1","role":"student"}}
```

Guarda el token:

```powershell
$token = "PEGA_AQUI_EL_TOKEN"
```

**Importante:** en todos los pasos siguientes usa el header:

```text
Authorization: Bearer TU_TOKEN
```

---

### 2) Crear sesión de acompañamiento

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions" ^
  -H "Authorization: Bearer $token" ^
  -H "Content-Type: application/json" ^
  -d "{\"locale\":\"es\",\"client\":\"unity\"}"
```

Respuesta: `session.id` (UUID). Guárdalo:

```powershell
$sessionId = "PEGA_AQUI_EL_SESSION_ID"
```

#### Error `SESSION_ALREADY_ACTIVE`

Si ya hay una sesión abierta en este PC, el servidor responde **409** con:

```json
{"error":{"code":"SESSION_ALREADY_ACTIVE","message":"Only one active session allowed on this node"}}
```

**Solución:** cierra la sesión anterior:

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/close" ^
  -H "Authorization: Bearer $token"
```

Luego vuelve a crear sesión. Si no tienes el `sessionId` viejo, pide ayuda a B o reinicia con `close` desde la DB.

---

### 3) Ver eventos (poll)

Consulta eventos nuevos. Repite hasta recibir `turn.result` (cuando subas audio).

```powershell
curl.exe -s "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/events?after=0" ^
  -H "Authorization: Bearer $token"
```

| Evento | Qué significa |
|--------|---------------|
| `session.ready` | Sesión lista |
| `turn.accepted` | Audio recibido |
| `turn.result` | Transcripción, respuesta, expresión y TTS |
| `turn.error` | Algo falló (reintentable) |

Usa `next_after` de la respuesta como `?after=` en la siguiente consulta.

---

### 4) Cerrar sesión

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/close" ^
  -H "Authorization: Bearer $token"
```

Respuesta: `{"ok":true,"session":{...}}` con `status: closed`.

---

## Problemas frecuentes

| Problema | Solución |
|----------|----------|
| Puerto 8000 ocupado | Cierra otra terminal con `artisan serve` o usa otro puerto |
| `401` en crear sesión | Falta header `Authorization: Bearer ...` o token expirado → login de nuevo |
| `SESSION_ALREADY_ACTIVE` | Cierra sesión anterior (ver arriba) |
| Health no responde | Verifica que `php artisan serve` está corriendo |
| `composer` / `php` no reconocido | Laragon → Start All → abre PowerShell nuevo |
| CORS (Unity WebGL) | En piloto local Unity Editor no suele necesitar CORS; avisa a B si falla |

---

## Verificación (B confirma hoy)

```powershell
# 1. Health
curl.exe -s http://127.0.0.1:8000/api/v1/health

# 2. Prueba de humo completa
cd C:\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

Debe terminar con: **`PHASE 0 SMOKE OK`**

| Verificación | Estado |
|--------------|--------|
| Health OK | ☐ confirmar hoy |
| Prueba de humo OK | ☐ confirmar hoy |

---

## Más documentación

- Aprendizaje del rol: [`APRENDIZAJE.md`](./APRENDIZAJE.md)
- Guía técnica: `../documentacion/equipo/guia-rol-B-servidor.md`
- Contrato REST: `../contratos/api-rest/v1/openapi.yaml`
- Misión Sprint 1: `../documentacion/aprendizaje/missions/sprint-1/M1-B-api-para-avatar.md`
