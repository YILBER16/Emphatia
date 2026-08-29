# Cliente Unity (módulo A)

Proyecto Unity 6 en `avatar/`.

## Pantalla de inicio de sesión

1. Abre el proyecto `avatar/` en **Unity Hub** (Unity 6).
2. Abre la escena `Assets/Scenes/Login.unity`.
3. En Hierarchy debe existir el objeto **EmpathiaLogin** (con `LoginScreenController`).
4. Pulsa **Play** ▶ (la UI se crea en Play, no en edición).
5. Debe aparecer la tarjeta **Inicio de sesión**. Si no, mira la Console por `[Empathia] LoginScreenController activo`.
4. Servidor: `http://IP_DE_B:8000/api/v1` (ej. `http://192.168.1.69:8000/api/v1`).
5. Usuario / contraseña lab: `estudiante1` / `password`.
6. **Iniciar sesión** → debes ver **Login OK** y un token parcial.

Scripts: `avatar/Assets/Scripts/Empathia/` (la UI se crea sola al dar Play).

## Sprint 2 — Turno audio

Tras el login: **Crear sesión** → **Turno WAV prueba** (o micrófono) → ver respuesta + TTS.

## Regla dura

Solo hablar con B (`:8000`). Nunca con Inteligencia (`:8100`).
