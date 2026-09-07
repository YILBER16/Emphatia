# Servidor (módulo B) — Laravel

API central de EmpathIA. Unity (A) habla **solo** con este servidor; el servidor llama a Inteligencia (C).

## Arrancar

```powershell
cd C:\Emphatia\backend
php artisan serve --host=127.0.0.1 --port=8000
```

Usuarios de prueba: `estudiante1` / `orientador1` / `admin1` — contraseña `password`.

### Admin — perfiles de estudiante (Fase 2)

Solo `admin1`. El estudiante **no** usa password. El `access_code` aparece **solo** al crear o regenerar.

```powershell
# Login admin
$login = Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"admin1","password":"password"}'
$h = @{ Authorization = "Bearer $($login.token)"; "Content-Type" = "application/json" }

# Crear perfil (guarda access_code de la respuesta)
$body = '{"nombres":"Ana","apellidos":"Perez","nombre_preferencia":"Anita","grado":"9-1","edad":15,"sede":"Norte","jornada":"tarde","documento_numero":"1098765432","acudiente_telefono":"3101112233","acudiente_documento":"52123456"}'
Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/admin/students" -Method POST -Headers $h -Body $body

# Listar / regenerar / desactivar
Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/admin/students" -Headers $h
# POST .../admin/students/{id}/regenerate-code
# POST .../admin/students/{id}/deactivate
```

ADR: `documentacion/decisiones/ADR-009-perfiles-estudiante-sin-password.md`.

### Adulto en Unity — lista y assume (Fase 3)

`admin` u `orientador` eligen estudiante (sin password del niño):

```powershell
$login = Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"orientador1","password":"password"}'
$h = @{ Authorization = "Bearer $($login.token)" }

# Lista activa (display_name, grado, sede…)
Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/students" -Headers $h

# Assume → token del estudiante (usar ese token en el resto del flujo)
$assumed = Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/students/1/assume" -Method POST -Headers $h
$studentHeaders = @{ Authorization = "Bearer $($assumed.token)"; "Content-Type" = "application/json" }
Invoke-RestMethod -Uri "http://127.0.0.1:8000/api/v1/accompaniment/sessions" -Method POST -Headers $studentHeaders -Body '{}'
```

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

### Enviar texto (sin audio)

Endpoint **nuevo**, independiente del login:

`POST /api/v1/accompaniment/sessions/{sessionId}/text`

Requiere token (después de login) y una sesión activa.

```powershell
$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
$body = '{"text":"Hola, me siento un poco cansado","client_turn_key":"550e8400-e29b-41d4-a716-446655440000"}'
Invoke-RestMethod -Uri "http://192.168.1.69:8000/api/v1/accompaniment/sessions/$sessionId/text" -Method POST -Headers $headers -Body $body
```

Luego consulta events hasta `turn.result`. El `transcript` será el texto que envió A.

Hoy A solo necesita login + sesión. El turno de audio lo conectarás en Sprint 2.

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
