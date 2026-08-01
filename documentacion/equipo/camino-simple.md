# EmpathIA — Camino simple (mentor + estudiantes)

Versión para equipo **no experto** + **vibe coding**.

## Metáfora (3 minutos)

| Persona | Rol | Carpeta |
|---------|-----|---------|
| Recepcionista | **B** Servidor | `servidor/` |
| Especialista | **C** Inteligencia | `inteligencia/` |
| Actor | **A** Avatar | `cliente-unity/` |
| Coach de cara | **D** Expresión | `expresion/` |

> El estudiante habla → Unity escucha → Servidor organiza → Inteligencia contesta → Unity habla y mueve la boca.

Regla de oro: **cada quien su carpeta**.

## Reunión día 1

Sigue el libreto: `documentacion/equipo/sesion-1-manana.md`

Hojas: `documentacion/equipo/hojas-rol/`

Prueba de humo:

```powershell
cd C:\laragon\www\Emphatia\servidor
php artisan serve --host=127.0.0.1 --port=8000
```

```powershell
cd C:\laragon\www\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

## Mensaje WhatsApp

```text
Hola equipo EmpathIA
No tienen que entender todo de golpe.

1) Cada uno tiene un rol (A Avatar / B Servidor / C Inteligencia / D Expresión)
2) Les paso una hoja de 1 página con su misión
3) Trabajamos con Cursor: misión clara + IA ayuda + ustedes prueban
4) Esta semana: UNA misión chiquita por persona
5) El viernes mostramos avances

El proyecto ya tiene base (prueba de humo). No partimos de cero.
Si se traban: preguntan. Está permitido no saber.
```
