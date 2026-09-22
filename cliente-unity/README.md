# Cliente Unity (módulo A)

Proyecto Unity 6 en `avatar/`.

**Ruta de este PC:** `D:\Emphatia\cliente-unity\avatar`  
Atajo: `.\cliente-unity\tools\abrir-unity.ps1`

## Pantalla de inicio de sesión

1. Abre el proyecto `D:\Emphatia\cliente-unity\avatar` en **Unity Hub** (Unity 6).
2. Abre la escena `Assets/Scenes/Login.unity`.
3. En Hierarchy debe existir el objeto **EmpathiaLogin** (con `LoginScreenController`).
4. Pulsa **Play** ▶ (la UI se crea en Play, no en edición).
5. Servidor: `http://192.168.1.31:8000/api/v1` (IP de B en el lab).
6. **Ingresar:** elige un nombre de la lista (B ya tiene el perfil).
7. **Registrar:** documento, nombre y apellido, sede, grado, jornada.
8. En Salud: texto o audio → respuesta + TTS. Si falla, sale un modal.

Scripts: `avatar/Assets/Scripts/Empathia/`

## Sprint 2 — Turno

Tras el login: **Crear sesión** (automático al enviar) → texto o **Grabar audio** → ver respuesta + TTS + boca mínima.

## Regla dura

Solo hablar con B (`:8000`). Nunca con Inteligencia (`:8100`).
