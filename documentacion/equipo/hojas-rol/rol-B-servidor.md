# EmpathIA — Hoja de rol · B (Servidor)

**Tu módulo:** API / organización del sistema  
**Tu carpeta:** `servidor/` (también puede verse como `backend/`; usa **servidor**)  
**Expones:** `http://127.0.0.1:8000/api/v1`  
**Puedes llamar a:** Inteligencia C → `http://127.0.0.1:8100`

---

## Eres responsable de

Login staff, **perfiles de estudiante** (solo admin), sesión, turnos, guardar historial/riesgo, pedir la respuesta a C, avisar a Unity.

## No haces

- Animación del avatar  
- Sustituir la IA dentro de Laravel  
- Dejar que A hable con C  

## Día 1

1. Lee `documentacion/equipo/guia-rol-B-servidor.md`.  
2. Arranca el servidor y corre `.\herramientas\prueba-humo-fase0.ps1`.  
3. Rama `b/api-para-a`.  
4. Escribe en `servidor/README.md` 5 ejemplos curl para A.

## Primera tarea

Documentar login, crear sesión, subir turno, ver eventos, cerrar sesión — en español claro.

## Usuarios de prueba

`estudiante1` / `orientador1` / `admin1` — contraseña: `password`  
(`estudiante1` es demo legado; el flujo real es adulto → lista → assume.)

## Perfiles (ADR-009)

- Solo **admin** crea / regenera código / desactiva.  
- **admin + counselor** listan y hacen `assume`.  
- Demo API: `.\herramientas\demo-perfiles-estudiante.ps1`

---

Guía larga: `documentacion/equipo/guia-rol-B-servidor.md`  
Checklists: `documentacion/aprendizaje/checklists/fase-perfiles/`
