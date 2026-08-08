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

## Guía rápida para Unity (A) — 5 ejemplos curl

Base URL: `http://127.0.0.1:8000/api/v1`

En Windows usa `curl.exe` (viene con el sistema). Después del login, guarda el `token` y úsalo en los demás pasos.

### 1) Login — obtener token

Entra con usuario y contraseña. La respuesta trae un `token` que dura 7 días.

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/auth/login" ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"estudiante1\",\"password\":\"password\"}"
```

Respuesta esperada (ejemplo):

```json
{"token":"abc123...","token_type":"Bearer","user":{"username":"estudiante1","role":"student"}}
```

Copia el valor de `token` y guárdalo en una variable:

```powershell
$token = "PEGA_AQUI_EL_TOKEN"
```

---

### 2) Crear sesión de acompañamiento

Abre una conversación nueva. Solo puede haber **una sesión activa** a la vez en este PC.

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions" ^
  -H "Authorization: Bearer $token" ^
  -H "Content-Type: application/json" ^
  -d "{\"locale\":\"es\",\"client\":\"unity\"}"
```

Respuesta: un `session.id` (UUID). Guárdalo:

```powershell
$sessionId = "PEGA_AQUI_EL_SESSION_ID"
```

---

### 3) Subir turno (audio del estudiante)

Envía un archivo WAV con lo que dijo el estudiante. `client_turn_key` debe ser un UUID único por turno (evita duplicados si reintentas).

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/turns" ^
  -H "Authorization: Bearer $token" ^
  -F "client_turn_key=550e8400-e29b-41d4-a716-446655440000" ^
  -F "audio=@C:\ruta\a\tu\audio.wav;type=audio/wav"
```

Respuesta: `turn.id` con status `accepted`. El procesamiento (STT → IA → TTS) ocurre en segundo plano; el resultado llega por **eventos** (paso 4).

---

### 4) Ver eventos (poll)

Consulta los eventos nuevos desde el último `after`. Repite hasta recibir `turn.result`.

```powershell
curl.exe -s "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/events?after=0" ^
  -H "Authorization: Bearer $token"
```

Eventos importantes:

| Tipo | Qué significa |
|------|---------------|
| `session.ready` | Sesión lista |
| `turn.accepted` | Audio recibido |
| `turn.result` | Transcripción, respuesta, expresión y URL de TTS |
| `turn.error` | Algo falló (reintentable) |

En `turn.result` encontrarás `transcript`, `reply_text`, `expression` (para D/A) y `tts.url` (audio de la respuesta).

Para la siguiente consulta usa `next_after` de la respuesta anterior como `?after=`.

---

### 5) Cerrar sesión

Termina la conversación cuando el estudiante se va.

```powershell
curl.exe -s -X POST "http://127.0.0.1:8000/api/v1/accompaniment/sessions/$sessionId/close" ^
  -H "Authorization: Bearer $token"
```

Respuesta: `{"ok":true,"session":{...}}` con status `closed`.

---

## Health check (sin login)

```powershell
curl.exe -s "http://127.0.0.1:8000/api/v1/health"
```

Debe devolver `"status":"ok"` (con `INTEL_STUB=true` no necesitas C corriendo).

## Prueba de humo automática

```powershell
cd C:\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

## Más documentación

- Aprendizaje del rol: [`APRENDIZAJE.md`](./APRENDIZAJE.md)
- Guía técnica: `../documentacion/equipo/guia-rol-B-servidor.md`
- Contrato REST: `../contratos/api-rest/v1/openapi.yaml`
