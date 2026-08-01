# STUDENT_HANDBOOK — Manual del estudiante EmpathIA

Este es tu **manual principal**.  
Léelo con calma la primera vez; luego úsalo como consulta.

Documentación técnica profunda: `documentacion/equipo/guia-rol-*.md`, `contratos/`, `documentacion/decisiones/`.

---

## 1. ¿Qué es EmpathIA?

Un **prototipo** de acompañamiento psicosocial para contextos educativos: un avatar 3D conversa por voz con un estudiante, con respuestas empáticas, y registra señales de seguimiento (no diagnostica).

Más visual: [PROJECT_MAP.md](./PROJECT_MAP.md).

---

## 2. ¿Cuál es el objetivo del proyecto?

Demostrar, en un PC Windows, un flujo completo de prototipo:

**escuchar → entender → responder → hablar → mover expresión**

con cuatro roles trabajando en paralelo sin pisarse.

No es (aún) un producto clínico ni multi-usuario concurrente.

---

## 3. ¿Cómo está organizado el repositorio?

```text
Emphatia/
  cliente-unity/     → Rol A
  servidor/          → Rol B   (puede verse también backend/)
  inteligencia/      → Rol C
  expresion/         → Rol D
  contratos/         → reglas de comunicación (todos cuidan)
  documentacion/     → docs (aprendizaje, equipo, manuales, ADRs)
  herramientas/      → scripts (ej. prueba de humo)
  datos/             → audio/logs locales (no secretos en Git)
```

**Tu regla:** modifica solo la carpeta de tu rol.

---

## 4. ¿Cómo trabajaremos?

1. Miro el [PROJECT_MAP](./PROJECT_MAP.md) y [ROLE_OVERVIEW](./ROLE_OVERVIEW.md).  
2. Entro al sprint actual en `missions/`.  
3. Cumplo la misión.  
4. Paso la `checklists/`.  
5. Demuestro en la Sprint Review.  

Metodología completa: [METODOLOGIA_EQUIPO.md](./METODOLOGIA_EQUIPO.md).

Usamos **Cursor (vibe coding)**: la IA ayuda a escribir; tú entiendes el objetivo, pruebas y no tocas carpetas ajenas.

---

## 5. ¿Qué significa un Sprint?

Un bloque de trabajo con meta clara (ej. Sprint 0 = solo encender entornos).

Al inicio: Planning.  
Al final: Review + checklist.  
Si no está Done, no fingimos que está Done.

Sprint 0: [missions/sprint-0/](./missions/sprint-0/).

---

## 6. ¿Cómo usar Git? (base)

Git guarda el historial del código.

Ideas mínimas:

- `main` = versión estable del equipo  
- Tu trabajo vive en una **rama**  
- Un **Pull Request (PR)** propone integrar tu rama a `main`  

Si aún no hay remoto GitHub: el mentor indica si trabajan por copia USB + más adelante Git. La disciplina de ramas igual se practica cuando exista remoto.

---

## 7. ¿Cómo crear una rama?

Con remoto configurado (ejemplo Rol A):

```text
git checkout main
git pull
git checkout -b a/m0-entorno-unity
```

Prefijos: `a/`, `b/`, `c/`, `d/` — ver `documentacion/equipo/colaboracion.md`.

---

## 8. ¿Cómo hacer un Pull Request?

1. Sube tu rama (`git push -u origin tu-rama`).  
2. En GitHub: Compare & pull request.  
3. Usa la plantilla de [METODOLOGIA_EQUIPO.md](./METODOLOGIA_EQUIPO.md) §5.  
4. Pide review al owner.  
5. Merge solo con checklist verde.

---

## 9. ¿Cómo reportar un bug?

Usa la mini-plantilla de metodología §8:

- pasos, esperado, obtenido, rol, misión, log/captura  

No “arregles” el módulo de otro sin permiso.

---

## 10. ¿Cómo solicitar cambios de contrato?

1. No edites `contratos/` a escondidas.  
2. Escribe qué quieres cambiar y por qué.  
3. Pide **Contract Review** (tú + módulos afectados + mentor).  
4. Si rompe compatibilidad: nueva versión (`v1` → `v2`), no silencio.

---

## 11. ¿Cómo documentar avances?

Al final de cada sesión, 3 bullets:

1. Qué hice  
2. Qué no entendí  
3. Qué necesito  

Además, mantén `APRENDIZAJE.md` en tu carpeta cuando la misión lo pida.

---

## 12. ¿Cómo preparar una entrega?

1. Checklist del sprint en verde.  
2. Evidencia lista (pantalla, archivo, health, humo).  
3. Guion de 2 minutos.  
4. Si dependes de B/C: avisa para tener el piloto arriba.

---

## 13. ¿Qué hacer cuando otro módulo cambia?

1. Lee el PR o el aviso del compañero.  
2. Si cambia un contrato que consumes: para y pide Contract Review / actualización.  
3. Si solo cambió por dentro (sin contrato): re-prueba tu integración en el piloto.  
4. Actualiza tu APRENDIZAJE si aprendiste un detalle nuevo.

---

## 14. Herramientas por rol (recordatorio)

| Rol | Herramientas mínimas Sprint 0 |
|-----|-------------------------------|
| Todos | Cursor, repo |
| A | Unity Hub / Unity 6 |
| B | Laragon (PHP), Composer |
| C | Python 3 |
| D | Cursor (JSON) |

Arranque: `documentacion/manuales/arranque-parada.md`, `puertos.md`.

---

## 15. Prueba de humo

Script: `herramientas/prueba-humo-fase0.ps1`  
Sirve para verificar el **esqueleto** A←B (con stub) sin Unity.

Éxito: mensaje `PHASE 0 SMOKE OK`.

---

## 16. Preguntas frecuentes

**¿Puedo usar código que Cursor inventó en otra carpeta?**  
No. Solo tu ownership.

**¿El proyecto es diagnóstico médico?**  
No. Son señales de acompañamiento; ver lenguaje en arquitectura/guías.

**¿Dónde está “la verdad” del API?**  
`contratos/` — no un chat informal.

**¿Las clases del mentor reemplazan este handbook?**  
No. Las clases (`documentacion/equipo/clase-*.md`) son guiones de sesión; este manual es la referencia estable.

---

## 17. Tu checklist de onboarding (primera semana)

- [ ] PROJECT_MAP leído  
- [ ] ROLE_OVERVIEW leído  
- [ ] M0-COMUN hecha  
- [ ] Misión M0 de tu rol hecha  
- [ ] Checklist Sprint 0 de tu rol verde  
- [ ] Participaste en integración de equipo  

---

## 18. Dónde pedir ayuda

1. Tu misión (sección errores comunes)  
2. Guía técnica de tu rol  
3. Compañero del puente (A↔B, A↔D, B↔C)  
4. Mentor  

Bienvenido/a al equipo EmpathIA.
