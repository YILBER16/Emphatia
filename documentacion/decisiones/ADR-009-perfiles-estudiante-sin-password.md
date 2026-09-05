# ADR-009 — Perfiles de estudiante sin usuario/contraseña

## Estado

Aceptado e implementado (Fases 0–5 — 2026-09-05).

## Contexto

EmpathIA acompaña a estudiantes en contexto escolar. Pedir usuario/contraseña al niño no es el flujo deseado. El administrador institucional debe crear perfiles; Unity (A) debe entrar tras un login de adulto y elegir al estudiante de una lista.

Hoy B solo autentica usuarios seed (`estudiante1` / `password`) vía `POST /auth/login`. No hay alta de perfiles ni entrada sin password.

## Decisión

1. **Solo `admin` crea y regenera** perfiles de estudiante.
2. El estudiante **no** tiene usuario/contraseña de acceso.
3. Cada perfil incluye datos mínimos escolares y de acudiente (ver abajo).
4. Existe un **`access_code` regenerable** (no permanente) para auditoría / acceso alternativo; la UI de A **no** se basa en teclear el código.
5. En A: **login de adulto** → **lista de estudiantes activos** → asumir identidad / abrir sesión.
6. Se mantiene `estudiante1` / `password` en seed **solo para demos de desarrollo**.
7. Datos de perfil viven en tabla dedicada `student_profiles` (1:1 con `users` role=student), para no mezclar credenciales de staff con datos del menor.
8. Listar / elegir estudiante para operar en lab: `admin` y `counselor`. **Crear / editar / regenerar código / desactivar:** solo `admin`.

### Campos mínimos del perfil

| Campo | Descripción |
|-------|-------------|
| `nombres` | Nombres del estudiante |
| `apellidos` | Apellidos |
| `nombre_preferencia` | Cómo prefiere que lo llamen (avatar) |
| `grado` | Grado / curso |
| `edad` | Edad |
| `sede` | Sede del colegio |
| `jornada` | Jornada (mañana / tarde / única, etc.) |
| `documento_numero` | Documento del estudiante |
| `acudiente_telefono` | Teléfono del acudiente |
| `acudiente_documento` | Documento del acudiente |
| `access_code` | Código regenerable (único) |
| `is_active` | Activo / inactivo (no borrar historial) |

`display_name` en API = `nombre_preferencia` (fallback: nombres + apellidos).

### Flujo acordado

```text
Admin (password) → CRUD perfiles + regenerar access_code

Adulto en Unity (admin o counselor, password)
  → GET lista estudiantes activos
  → Elige estudiante
  → B emite token de ese estudiante (assume)
  → Sesión / texto / events (igual que hoy)
```

## Consecuencias

- Hay que migrar schema + endpoints admin + `assume` antes de cambiar la UI de A.
- Login clásico queda para staff (`admin`, `counselor`); no para el flujo principal del estudiante.
- Documentación de A/B y hojas de rol deben actualizarse cuando se implemente.
- No es SSO ni registro público; fuera de alcance borrar físico el historial.

## Fases

| Fase | Qué | Estado |
|------|-----|--------|
| 0 | Acuerdos | Hecho |
| 1 | Migración + modelos `student_profiles` | Hecho |
| 2 | API admin CRUD + regenerate-code | Hecho |
| 3 | Lista enriquecida + `POST /students/{id}/assume` | Hecho |
| 4 | Unity: login adulto → lista → assume → sesión | Hecho |
| 5 | Demo integrado + docs A/B + script | Hecho |

## Referencias

- `documentacion/aprendizaje/checklists/fase-perfiles/`
- `herramientas/demo-perfiles-estudiante.ps1`
- `backend/APRENDIZAJE.md` / `cliente-unity/APRENDIZAJE.md`
