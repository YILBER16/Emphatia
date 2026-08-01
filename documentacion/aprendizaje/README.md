# Capa de aprendizaje — EmpathIA

**Empieza aquí si eres estudiante.**  
Esta carpeta no reemplaza la documentación técnica: te guía para trabajar como equipo sin perder el nivel profesional del proyecto.

## Puerta de entrada (orden recomendado)

| Orden | Documento | Para qué |
|-------|-----------|----------|
| 1 | [PROJECT_MAP.md](./PROJECT_MAP.md) | Entender el sistema en ≤ 5 minutos |
| 2 | [ROLE_OVERVIEW.md](./ROLE_OVERVIEW.md) | Ver tu rol y límites en 1 página |
| 3 | [STUDENT_HANDBOOK.md](./STUDENT_HANDBOOK.md) | Manual del día a día |
| 4 | [METODOLOGIA_EQUIPO.md](./METODOLOGIA_EQUIPO.md) | Cómo funciona la “mini-empresa” |
| 5 | [missions/sprint-0/](./missions/sprint-0/) | Tu primera misión (solo entorno) |
| 6 | [checklists/sprint-0/](./checklists/sprint-0/) | Validar antes de decir “terminé” |

## Mapa de capas del proyecto

| Capa | Dónde | Qué responde |
|------|-------|--------------|
| **Aprendizaje** (esta) | `documentacion/aprendizaje/` | ¿Qué hago primero? ¿Cómo demuestro avance? |
| **Equipo / mentor** | `documentacion/equipo/` | Guías técnicas de rol, clases, colaboración |
| **Arquitectura** | `documentacion/arquitectura/`, `decisiones/` | Por qué el sistema es así |
| **Contratos** | `contratos/` | Formas exactas de hablar entre módulos |
| **Operación** | `documentacion/manuales/` | Cómo arrancar puertos y servicios |

## Regla de oro

Si la guía técnica y esta capa parecen decir cosas distintas sobre **arquitectura o contratos**, gana la capa técnica (`contratos/`, ADR, guías de rol).  
Esta capa solo organiza el **aprendizaje y el trabajo**.

## Roles y carpetas de código

| Rol | Carpeta que sí puedes modificar |
|-----|----------------------------------|
| A Avatar | `cliente-unity/` |
| B Servidor | `servidor/` (también puede verse `backend/`) |
| C Inteligencia | `inteligencia/` |
| D Expresión | `expresion/` (+ propuestas a `contratos/expresion/` con revisión) |
