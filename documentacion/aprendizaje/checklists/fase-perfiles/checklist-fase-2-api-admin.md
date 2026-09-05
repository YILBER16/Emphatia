# Checklist Fase 2 — API admin perfiles de estudiante

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

## Hecho

- [x] `AdminStudentController` (CRUD + regenerate + deactivate)
- [x] Rutas bajo `/api/v1/admin/students` (solo `admin`)
- [x] `access_code` solo en create / regenerate-code
- [x] Counselor recibe 403
- [x] Prueba manual create / list / patch / regenerate / deactivate OK

## Endpoints

| Método | Ruta |
|--------|------|
| GET | `/api/v1/admin/students` |
| POST | `/api/v1/admin/students` |
| GET | `/api/v1/admin/students/{id}` |
| PATCH | `/api/v1/admin/students/{id}` |
| POST | `/api/v1/admin/students/{id}/regenerate-code` |
| POST | `/api/v1/admin/students/{id}/deactivate` |

## Siguiente

- [ ] Fase 3 — lista enriquecida para adulto + `POST /students/{id}/assume`

**Fecha:** 2026-09-05
