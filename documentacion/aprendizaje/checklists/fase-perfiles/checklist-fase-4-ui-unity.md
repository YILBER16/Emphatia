# Checklist Fase 4 — UI Unity adulto → lista → assume

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

## Hecho

- [x] `ListStudents` + `AssumeStudent` en `EmpathiaApiClient`
- [x] Estado: `AdultToken`, `Role`, `StudentDisplayName`
- [x] Pantalla **Elegir estudiante** tras login admin/orientador
- [x] Demo legado: `estudiante1` sigue yendo a Confirm sin lista
- [x] Base URL por defecto: `http://192.168.1.31:8000/api/v1`

## Cómo probar en Unity

1. B arriba en `192.168.1.31:8000`
2. Play → login `orientador1` / `password`
3. Debe aparecer lista (al menos `Estudiante Uno`)
4. Elegir → Confirm → Salud → enviar texto

## Siguiente

- [ ] Fase 5 — demo integrado + README A/B alineados

**Fecha:** 2026-09-05
