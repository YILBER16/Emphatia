# EmpathIA

Prototipo de acompañamiento psicosocial con avatar 3D (un PC Windows).

## Nombres en español (importante)

Trabajamos las carpetas del proyecto en español para el equipo:

| Rol | Carpeta | Quién |
|-----|---------|-------|
| A — Avatar (Unity) | `cliente-unity/` | Estudiante A |
| B — Servidor (Laravel) | `backend/` (enlace local opcional: `servidor/`) | Estudiante B |
| C — Inteligencia | `inteligencia/` | Estudiante C |
| D — Expresión | `expresion/` | Estudiante D |
| Contratos compartidos | `contratos/` | Todos |
| Documentación | `documentacion/` | Todos |
| Herramientas | `herramientas/` | Todos |
| Datos locales (audio, logs) | `datos/` | (no va a Git) |

**Nota:** en GitHub la carpeta Laravel se llama **`backend/`**.  
En Windows del equipo pueden crear un enlace con nombre en español:

```powershell
cmd /c "mklink /J servidor backend"
```

Luego pueden usar `servidor/` como en la documentación. Si no crean el enlace, usen siempre `backend/`.

Lo interno de Laravel (`app/`, `routes/`, etc.) queda en inglés: así lo exige el framework.

## Empezar (estudiantes)

**Puerta principal de aprendizaje:** [`documentacion/aprendizaje/`](documentacion/aprendizaje/)

1. [PROJECT_MAP.md](documentacion/aprendizaje/PROJECT_MAP.md) (5 min)  
2. [ROLE_OVERVIEW.md](documentacion/aprendizaje/ROLE_OVERVIEW.md)  
3. [STUDENT_HANDBOOK.md](documentacion/aprendizaje/STUDENT_HANDBOOK.md)  
4. Sprint 0: [`documentacion/aprendizaje/missions/sprint-0/`](documentacion/aprendizaje/missions/sprint-0/)  

## Empezar (mentor / clases)

1. Mentor: [`documentacion/equipo/clase-de-hoy.md`](documentacion/equipo/clase-de-hoy.md)  
2. Camino simple: [`documentacion/equipo/camino-simple.md`](documentacion/equipo/camino-simple.md)  
3. Hojas de rol: [`documentacion/equipo/hojas-rol/`](documentacion/equipo/hojas-rol/)  
4. Guías técnicas de rol (intactas): `documentacion/equipo/guia-rol-*.md`  

## Probar que la base funciona (prueba de humo)

```powershell
cd C:\laragon\www\Emphatia\servidor
php artisan serve --host=127.0.0.1 --port=8000
```

En otra terminal:

```powershell
cd C:\laragon\www\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

Debe terminar con: `PHASE 0 SMOKE OK`

## Puertos y arranque

Ver [`documentacion/manuales/`](documentacion/manuales/).
