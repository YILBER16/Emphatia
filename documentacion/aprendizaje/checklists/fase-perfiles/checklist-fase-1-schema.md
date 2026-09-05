# Checklist Fase 1 — Schema perfiles de estudiante

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

## Hecho

- [x] Migración `student_profiles`
- [x] `users.password` nullable
- [x] Modelo `StudentProfile` + `User::studentProfile()`
- [x] Seed: perfil demo de `estudiante1` (`access_code=DEMO01`)
- [x] `php artisan migrate` + `db:seed` OK

## Siguiente

- [ ] Fase 2 — API admin CRUD + regenerate-code

**Fecha:** 2026-09-05
