# Aprendizaje — Rol A (Avatar / Unity)

**Misión actual:** M2-A · Turno con audio  
**Carpeta de trabajo:** `cliente-unity/`

---

## Qué es mi rol

Soy el **Rol A — Avatar**. Soy la cara del sistema EmpathIA: el estudiante me ve, me habla y me oye.

**No “pienso”.** Solo consumo lo que entrega el **Servidor B** (`:8000`). Nunca llamo a Inteligencia (`:8100`).

---

## Estructura

```text
cliente-unity/avatar/Assets/Scripts/Empathia/
├── EmpathiaAuthState.cs       ← token + session.id
├── EmpathiaApiModels.cs       ← DTOs JSON
├── EmpathiaApiClient.cs       ← login / sesión / turno / events / TTS
├── EmpathiaWav.cs             ← WAV prueba + mic → bytes
└── LoginScreenController.cs   ← UI (bootstrap al Play)
```

---

## Pantalla de login (autenticación)

Escena: `avatar/Assets/Scenes/Login.unity`

**Flujo principal (Fase 4 — sin password del niño):**

1. Abrir escena **Login** → **Play**
2. Servidor: `http://192.168.1.31:8000/api/v1` (o la IP de B)
3. Login adulto: `orientador1` / `password` (o `admin1`)
4. Pantalla **Elegir estudiante** → tocar un perfil activo
5. Confirm → Salud → texto/audio a B

**Demo legado:** `estudiante1` / `password` salta la lista (solo lab).

Los perfiles los crea el admin en B (`POST /api/v1/admin/students`).

## Sprint 2 — Flujo del turno

1. B arriba (LAN ejemplo): `http://192.168.1.58:8000/api/v1`  
   En el PC de B: `php artisan serve --host=0.0.0.0 --port=8000`
2. Escena Login → Play
3. **Iniciar sesión** → **Crear sesión**
4. **Turno WAV prueba** (o micrófono 3s)
5. Poll de `GET .../events` hasta `turn.result` (o `turn.error` / timeout)
6. Mostrar `reply_text` + reproducir TTS con Bearer

### Estados UI

`idle` → `listening` (preparar/grabar audio) → `processing` (upload + poll) → `speaking` (TTS) → `idle`

### Endpoints (solo B)

| Acción | Método |
|--------|--------|
| Login | `POST /api/v1/auth/login` |
| Sesión | `POST /api/v1/accompaniment/sessions` |
| Turno | `POST .../sessions/{id}/turns` multipart (`audio` + `client_turn_key`) |
| Events | `GET .../sessions/{id}/events?after=` |
| TTS | `GET .../turns/{turnId}/audio/tts` + Bearer |
| Cerrar | `POST .../sessions/{id}/close` |

La URL de TTS se arma con el **mismo host** de la Base URL (evita `127.0.0.1` cuando B está en LAN).

### Errores

| Situación | Qué hago |
|-----------|----------|
| Timeout sin result | Revisar poll/`after` con B; stub C / `INTEL_STUB` |
| TTS 401 | Header Authorization en download |
| SESSION_ALREADY_ACTIVE | Cerrar sesión y recrear |
| Mic falla | Usar «Turno WAV prueba» |

### Nota de prueba A↔B (turno)

- **Fecha:** 2026-08-11
- **Base URL usada:** `http://192.168.1.58:8000/api/v1` (default en el cliente)
- **Resultado:** login + sesión OK contra B en LAN
- **UI:** responsiva (CanvasScaler + scroll + filas apiladas en pantallas estrechas)
- **Si falla:** anotar `error.code` / mensaje UI

---

## Sprint 1 (hecho en código)

Login + sesión + token parcial + errores en español.

---

## Carpetas que no toco

| Carpeta | Por qué |
|---------|---------|
| `backend/` | Es de B |
| `inteligencia/` | Es de C |
| `expresion/` | Es de D |
| `contratos/` | Solo con review |
