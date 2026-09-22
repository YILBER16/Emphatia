# Aprendizaje — Rol A (Avatar / Unity)

**Misión actual:** M2-A · Turno con audio  
**Carpeta de trabajo:** `D:\Emphatia\cliente-unity\`  
Abrir Unity: `.\cliente-unity\tools\abrir-unity.ps1`

---

## Qué es mi rol

Soy el **Rol A — Avatar**. Soy la cara del sistema EmpathIA: el estudiante me ve, me habla y me oye.

**No “pienso”.** Solo consumo lo que entrega el **Servidor B** (`:8000`). Nunca llamo a Inteligencia (`:8100`).

---

## Estructura

```text
cliente-unity/avatar/Assets/Scripts/Empathia/
├── EmpathiaAuthState.cs       ← token + session.id + preferred_name corto
├── EmpathiaApiModels.cs       ← DTOs JSON (incluye ExpressionPacket)
├── EmpathiaApiClient.cs       ← login / sesión / turno / events / TTS
├── EmpathiaWav.cs             ← WAV prueba + mic → bytes
├── EmpathiaMouthDriver.cs     ← boca mínima en speaking
└── LoginScreenController.cs   ← UI (bootstrap al Play)
```

---

## Pantalla de login (hoy)

Escena: `avatar/Assets/Scenes/Login.unity`  
Servidor por defecto: `http://192.168.1.31:8000/api/v1`

1. Abrir escena **Login** → **Play** (la UI se crea sola).
2. **Ingresar:** lista de nombres desde B. Eliges uno (el perfil completo queda en memoria).
3. **Registrar:** documento, nombre y apellido, sede, grado, jornada. A crea el perfil en B con admin.
4. Confirm → Salud → texto o audio a B.

No hay pestaña Adulto. No pedimos contraseña al estudiante.  
`preferred_name` se recorta a 1–2 palabras (máx. 40) para que C no rechace el turno.

---

## Sprint 2 — Flujo del turno

1. B arriba: `http://192.168.1.31:8000/api/v1`  
   En el PC de B: `php artisan serve --host=0.0.0.0 --port=8000`
2. Escena Login → Play → elegir estudiante
3. En Salud: **Enviar texto a B** o **Grabar audio**
4. Poll de `GET .../events` hasta `turn.result` (o `turn.error` / timeout)
5. Mostrar `reply_text` + reproducir TTS con Bearer
6. Estado `speaking`: boca UI (y blendshape `jawOpen` si el avatar lo tiene)
7. Si hay `turn.error`, modal en español con el código/mensaje de B

### Estados UI

`idle` → `listening` (preparar/grabar audio) → `processing` (upload + poll) → `speaking` (TTS + boca) → `idle`

### Endpoints (solo B)

| Acción | Método |
|--------|--------|
| Lista | `GET /api/v1/admin/students?active_only=1` |
| Registro | `POST /api/v1/admin/students` |
| Assume | `POST /api/v1/students/{id}/assume` |
| Sesión | `POST /api/v1/accompaniment/sessions` |
| Texto | `POST .../sessions/active/text` (`preferred_name` corto) |
| Turno audio | `POST .../sessions/{id}/turns` multipart (`audio` + `client_turn_key`) |
| Events | `GET .../sessions/{id}/events?after=` |
| TTS | `GET .../turns/{turnId}/audio/tts` + Bearer |
| Cerrar | `POST .../sessions/{id}/close` |

La URL de TTS se arma con el **mismo host** de la Base URL (evita `127.0.0.1` cuando B está en LAN).

### Errores

| Situación | Qué hago |
|-----------|----------|
| `turn.error` | Modal con `MapTurnError` (código + mensaje). No invento reply. |
| Timeout sin result | Revisar poll/`after` con B |
| TTS 401 | Header Authorization en download |
| SESSION_ALREADY_ACTIVE | Cerrar sesión y recrear |
| Mic falla | Escribir texto o usar WAV de prueba |
| Nombre largo | A ya recorta `preferred_name` |

### Nota de prueba A↔B (turno)

- **Fecha:** 2026-09-22
- **Base URL:** `http://192.168.1.31:8000/api/v1`
- **Hecho en A:** login por lista, registro, texto/audio, poll, TTS, error modal, boca mínima
- **Pendiente de demo en vivo:** oír TTS con B+C arriba (si C está en stub, el texto es genérico)
- **Si falla:** anotar el texto del modal y pasárselo a B

### Boca / D

No hay mesh con blendshapes en la escena Login. En `speaking` se mueve un óvalo de UI.  
Si D pone un avatar con `jawOpen` / `mouthOpen`, `EmpathiaMouthDriver` lo usa solo.  
Tabla morphs y pairing fino: con D (M2-D). `timing_quality: low` es aceptable.

---

## Sprint 1 (hecho)

Login + sesión + token + errores en español.

---

## Carpetas que no toco

| Carpeta | Por qué |
|---------|---------|
| `backend/` | Es de B |
| `inteligencia/` | Es de C |
| `expresion/` | Es de D |
| `contratos/` | Solo con review |
