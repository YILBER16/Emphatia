# METODOLOGIA_EQUIPO — Mini-empresa EmpathIA

Cómo trabajamos como un equipo de software profesional, adaptado a estudiantes de secundaria.  
Complementa `documentacion/equipo/colaboracion.md` (reglas Git/ownership ya existentes).

---

## 1. Principios

1. **Contratos primero** — la forma de hablar entre módulos está en `contratos/`.  
2. **Ownership** — cada rol mergea solo su carpeta.  
3. **Evidencia** — “terminé” = checklist + demo corta.  
4. **Capa técnica gana** — si hay duda de arquitectura, mandan ADRs/contratos/guías de rol.  
5. **Prototipo con disciplina** — no improvisar romper límites “porque Cursor lo sugirió”.

---

## 2. Sprints

Un **Sprint** es una ventana de trabajo con objetivo cerrado (ej. una sesión larga de 3h30 o una semana lectiva).

| Ritual | Cuándo | Duración | Quién |
|--------|--------|----------|-------|
| **Sprint Planning** | Inicio | 15–25 min | Todos + mentor |
| **Trabajo / 1:1** | Durante | según clase | Mentor + cada rol |
| **Integration** | Mitad o viernes | 20–40 min | Todos en PC piloto |
| **Sprint Review** | Cierre | 20–30 min | Todos demuestran |
| **Retrospectiva corta** | Tras review | 10 min | Qué mejorar mañana |
| **Contract Review** | Solo si cambia contrato | 15–30 min | Productor + consumidores |

### Sprint Planning (agenda)

1. Leer objetivo del sprint (`missions/sprint-N/README.md`).  
2. Cada uno confirma su misión.  
3. Señalar bloqueos de entorno.  
4. Acordar PC piloto.

### Sprint Review (agenda)

1. Cada rol: 2 min + checklist.  
2. Checklist de integración.  
3. Mentor declara Sprint DONE / NO-DONE.  
4. Anunciar siguiente sprint (sin empezar features si el actual no cerró).

---

## 3. Definition of Ready (DoR) — “¿podemos empezar?”

Una misión está **Ready** si:

- [ ] Tiene archivo de misión completo (objetivo, aceptación, NO hacer)  
- [ ] El estudiante terminó el setup mínimo del sprint anterior (o M0-COMUN)  
- [ ] No depende de un cambio de contrato no aprobado  
- [ ] Está claro qué carpetas puede tocar  
- [ ] Hay forma de validar (pasos de validación escritos)

Si falta DoR → no se empieza “a ciegas”; se desbloquea con mentor.

---

## 4. Definition of Done (DoD) — “¿está terminado?”

Una misión/sprint está **Done** si:

- [ ] Criterios de aceptación en verde  
- [ ] Checklist del rol en verde  
- [ ] Evidencia demostrable en ≤ 2 minutos  
- [ ] No se rompió la prueba de humo del piloto (si el cambio podía afectarla)  
- [ ] Documentación mínima del estudiante actualizada (`APRENDIZAJE.md` cuando aplique)  
- [ ] Si hubo código: PR revisado por owner de carpeta (y consumidores si toca contrato)

**Hecho a medias no es Done.** Se documenta como “en progreso” + bloqueo.

---

## 5. Flujo Git recomendado

```text
main (siempre estable / humo OK)
  └── a/m0-entorno
  └── b/m0-entorno
  └── c/m0-entorno
  └── d/m0-entorno
```

1. `git pull` en `main` (o copiar repo si aún no hay remoto).  
2. Crear rama: `a/...`, `b/...`, `c/...`, `d/...`.  
3. Commits pequeños con mensaje claro en español o inglés consistente.  
4. Abrir **Pull Request** hacia `main`.  
5. Owner de la carpeta aprueba.  
6. Merge solo si checklist OK.

### Pull Request — plantilla mínima

```text
## Misión
M0-...

## Qué cambié
- ...

## Cómo probar
1. ...

## Checklist
- [ ] Checklist sprint de mi rol en verde
- [ ] No toqué carpetas ajenas
- [ ] No cambié contratos (o adjunto Contract Review)
```

Detalle de branches: `documentacion/equipo/colaboracion.md`.

---

## 6. Revisión de código

- El student pide review al **owner** (en Sprint 0, el mentor puede co-revisar).  
- Se revisa: ¿cumple aceptación?, ¿tocó de más?, ¿rompible el piloto?  
- Cursor puede generar código; el humano es responsable de lo que entra a `main`.

---

## 7. Integración

- **PC piloto** único para demos.  
- Ritual: arrancar B (+ C si aplica) y correr prueba de humo.  
- Si A/D integran: hacerlo sobre API estable de B, no forks silenciosos.

---

## 8. Manejo de incidencias (bugs)

1. Reproducir en 3 pasos.  
2. Anotar: rol, PC, mensaje de error, misión activa.  
3. Reportar al owner del módulo sospechoso (+ mentor).  
4. No “arreglar” carpeta ajena sin permiso.

### Mini-plantilla de bug

```text
Título:
Rol que reporta:
Misión:
Pasos:
Esperado:
Obtenido:
Captura/log:
```

---

## 9. Comunicación entre estudiantes

| Necesitas… | Habla con… | Canal |
|------------|------------|-------|
| Login / API | B | Pareja A↔B |
| Morphs / boca | D | Pareja A↔D |
| InferTurn / stub | C | Pareja B↔C |
| Cambio de JSON contrato | Contract Review | Mentoría + afectados |
| Duda de arquitectura | Mentor | No inventar ADR |

---

## 10. Revisión de contratos

- Prohibido mergear breaking changes en silencio.  
- Flujo: propuesta → Contract Review → bump de versión si rompe → luego implementar.  
- Referencia: `contratos/README.md`.

---

## 11. Documentar avances

Al final de cada sesión, 3 bullets:

1. Qué hice  
2. Qué no entendí  
3. Qué necesito  

Opcional: actualizar `APRENDIZAJE.md` del módulo.

---

## 12. Preparar una entrega / review

1. Checklist en verde.  
2. Rama/PR o carpeta lista.  
3. Guion de 2 minutos.  
4. Servidor/piloto listo si tu demo depende de él.

---

## Relación con sprints de aprendizaje

| Artefacto | Uso |
|-----------|-----|
| `missions/sprint-N` | Qué hacer |
| `checklists/sprint-N` | Cómo saber que terminaste |
| Esta metodología | Cómo se comporta el equipo |
