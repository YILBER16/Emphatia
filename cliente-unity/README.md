# Cliente Unity (módulo A)

Proyecto Unity 6 en `avatar/`.

**Ruta de este PC (disco D):** `D:\Emphatia\cliente-unity\avatar`  
No abras `C:\Emphatia\...` (copia vieja). Atajo: `.\cliente-unity\tools\abrir-unity.ps1`

## Pantalla de inicio de sesión

1. Abre el proyecto `D:\Emphatia\cliente-unity\avatar` en **Unity Hub** (Unity 6).
2. Abre la escena `Assets/Scenes/Login.unity`.
3. En Hierarchy debe existir el objeto **EmpathiaLogin** (con `LoginScreenController`).
4. Pulsa **Play** ▶ (la UI se crea en Play, no en edición).
5. Debe aparecer la tarjeta **Inicio de sesión**. Si no, mira la Console por `[Empathia] LoginScreenController activo`.
4. Servidor: `http://127.0.0.1:8000/api/v1` (mismo PC). Si B está en otro PC: `http://IP_DE_B:8000/api/v1`.
5. Pestaña **Adulto** (flujo actual de B): `orientador1` / `password` → elige un estudiante de la lista.
6. Pestaña **Estudiante**: UI lista; el ingreso por documento espera la ruta de B. Mientras tanto usa Adulto.

Scripts: `avatar/Assets/Scripts/Empathia/` (la UI se crea sola al dar Play).

## Sprint 2 — Turno audio

Tras el login: **Crear sesión** → **Turno WAV prueba** (o micrófono) → ver respuesta + TTS.

## Regla dura

Solo hablar con B (`:8000`). Nunca con Inteligencia (`:8100`).
