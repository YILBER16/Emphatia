# Checklist Fase 0 — Acuerdos perfiles de estudiante

Decisiones para habilitar creación de perfiles por admin **sin** usuario/contraseña del estudiante.

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

## Acuerdos

- [x] Solo **admin** crea perfiles (counselor no crea)
- [x] Campos mínimos: nombres, apellidos, grado, edad, sede, jornada, nombre de preferencia, documento estudiante, teléfono acudiente, documento acudiente
- [x] `access_code` **se regenera** (no es permanente)
- [x] UI de A: **lista tras login de adulto** (no campo código como flujo principal)
- [x] Mantener `estudiante1` / `password` en seed solo para demos
- [x] Estudiante **sin** password de acceso
- [x] Listar/elegir: admin + counselor; CRUD/regenerar/desactivar: solo admin
- [x] Tabla `student_profiles` 1:1 con `users` (student)

## Siguiente

- [ ] Fase 1 — Migración + modelos (B)

**Fecha acuerdos:** 2026-09-05  
**Rol B:** ________  
**Validado con equipo (A):** ________
