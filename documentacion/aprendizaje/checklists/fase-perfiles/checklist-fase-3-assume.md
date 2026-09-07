# Checklist Fase 3 — Lista adulto + assume

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

## Hecho

- [x] `GET /api/v1/students` — lista enriquecida, solo `is_active` (admin + counselor)
- [x] `POST /api/v1/students/{id}/assume` — token Bearer del estudiante
- [x] Login staff rechaza `password` null (estudiantes sin clave)
- [x] Inactivo → `STUDENT_INACTIVE` (422)
- [x] Estudiante no puede listar ni assume (403)
- [x] Tras assume se puede crear sesión de acompañamiento

## Flujo para A

```text
1. Login adulto (admin1 u orientador1)
2. GET /students  → elegir id
3. POST /students/{id}/assume → token del estudiante
4. POST /accompaniment/sessions (con token estudiante)
5. Texto / events como hoy
```

## Siguiente

- [ ] Fase 4 — UI Unity: login adulto → lista → assume → sesión

**Fecha:** 2026-09-05
