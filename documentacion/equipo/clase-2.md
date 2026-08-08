# Clase 2 — EmpathIA (después de Sprint 0)

**Duración:** 3 h 30 (adaptable a 2 h)  
**Premisa:** la clase 1 instaló entorno / Sprint 0. Hoy **avanzamos el prototipo**: cada rol produce una pieza real del flujo EmpathIA y se conectan en puentes.

**Sprint de aprendizaje:** [Sprint 1](../aprendizaje/missions/sprint-1/)

---

## Meta del día (una frase)

> “Que A hable de verdad con B; que C explique y pruebe un InferTurn stub; que D deje morphs listos para A; que el equipo vea el mapa moviéndose.”

## Qué avanza de EmpathIA hoy

| Pieza del sistema | Quién | Avance concreto |
|-------------------|-------|-----------------|
| Login / sesión | A + B | Token real + sesión creada |
| Turno (esqueleto) | B (+ A si da tiempo) | A entiende events; B documenta flujo turno |
| InferTurn | C | Lectura + llamada de prueba al stub |
| Expresión | D + A | Tabla morphs acordada |
| Integración | Todos | Demo corta al cierre |

**Aún NO:** Whisper, Ollama, Kokoro, lip-sync fino, WebSockets reales, MySQL obligatorio.

---

## Reloj 3 h 30

| Bloque | Tiempo | Qué |
|--------|--------|-----|
| 0. Tú solo | antes | Humo OK; saber qué faltó de Sprint 0 |
| 1. Arranque | **0:00–0:25** | Recuperar rezagados + pull del repo |
| 2. Grupal | **0:25–0:50** | Meta del día + puentes + abrir Sprint 1 |
| 3. 1:1 | **0:50–1:35** | B→A→C→D (~10–12 min) |
| 4. Trabajo | **1:35–2:35** | Misiones Sprint 1 |
| 5. Parejas | **2:35–3:10** | A↔B, A↔D, mini B↔C |
| 6. Cierre | **3:10–3:30** | Demo 2 min c/u + checklist |

**Si solo hay 2 h:** bloques 1–3 + 30 min trabajo + cierre (parejas mínimas A↔B).

---

## Parte 0 — Antes de clase (tú)

- [ ] `git pull` en el PC demo  
- [ ] Servidor: health OK + prueba de humo OK  
- [ ] Lista: quién cerró Sprint 0 / quién debe terminar setup primero  
- [ ] Abrir: este archivo + `documentacion/aprendizaje/missions/sprint-1/`  

Repo: https://github.com/YILBER16/Emphatia

---

# BLOQUE 1 — Arranque (25 min)

### 1.1 Pull / sync (todos)

```powershell
cd ruta\Emphatia
git pull origin main
```

Si no usan Git aún: copiar actualización del mentor.

### 1.2 Tablero Sprint 0 → Sprint 1

| | A | B | C | D |
|--|---|---|---|---|
| Sprint 0 checklist OK | | | | |
| Herramienta abre hoy | Unity | artisan | simulador | JSON |

Quien no terminó Sprint 0: **20 min máximo** a cerrarlo; el resto empieza Sprint 1.

---

# BLOQUE 2 — Grupal (25 min)

### 2.1 Repaso 1 minuto

Dibuja de nuevo:

```text
Estudiante → A → B → C → (packet D) → A
```

Pregunta: “¿A puede llamar a C?” → **No.**

### 2.2 Meta Sprint 1

Abrir en proyector: `documentacion/aprendizaje/missions/sprint-1/README.md`

> “Sprint 0 fue encender. Sprint 1 es **conectar**.  
> Cada uno tiene misión M1. Al final hay checklist.”

### 2.3 Los puentes de hoy (pizarra)

```text
A  ←── login + sesión ──→  B
A  ←── tabla morphs ────→  D
B  ←── stub InferTurn ──→  C
```

### 2.4 Éxito visible al cierre

| Rol | Debe poder mostrar |
|-----|-------------------|
| B | README “Para A” + health + (ideal) humo |
| A | Login OK con token real (o error claro) |
| C | APRENDIZAJE InferTurn + prueba al stub |
| D | Tabla morphs con columna sí/no de A |

---

# BLOQUE 3 — 1:1 (B → A → C → D)

En cada 1:1: abrir su `M1-*.md` + checklist + prompt Cursor.

---

## Ficha B — Servidor

**Dile:** “Hoy dejas el idioma listo para que A no adivine.”

**Misión:** [M1-B](../aprendizaje/missions/sprint-1/M1-B-api-para-avatar.md)

**Prompt Cursor:**

```text
Somos el módulo B (carpeta backend/ o servidor/).
Objetivo Sprint 1:
1) Verificar php artisan serve y GET /api/v1/health
2) Mejorar README con sección "Para el compañero A (Unity)" en español:
   - cómo arrancar el servidor
   - curl/login con estudiante1 / password
   - cómo crear sesión
   - qué es el token y dónde se usa
   - enlace a events (poll) y qué significa turn.result
3) Pegar un ejemplo real o de ejemplo del JSON de login
4) NO implementes WebSockets ni MySQL hoy
Explica en 5 líneas qué agregaste.
```

---

## Ficha A — Avatar

**Dile:** “Hoy el login deja de ser mentira: hablas con B.”

**Misión:** [M1-A](../aprendizaje/missions/sprint-1/M1-A-login-sesion.md)

**Prompt Cursor:**

```text
Somos el módulo A (cliente-unity/).
Objetivo Sprint 1:
1) Conectar Entrar a POST http://127.0.0.1:8000/api/v1/auth/login
   body: {"username":"estudiante1","password":"password"}
2) Guardar el token
3) Mostrar "Login OK" + primeros caracteres del token
4) Botón o paso siguiente: crear sesión
   POST /api/v1/accompaniment/sessions  (Authorization Bearer)
5) Si falla: mensaje en español claro
Usa el README de B si existe.
NO llames a inteligencia/ (:8100).
Explica el flujo en 5 líneas.
```

**Si Unity incompleto:** misma lógica en un script C# + prueba con curl guiada; UI mínima.

---

## Ficha C — Inteligencia

**Dile:** “Hoy demuestras el cerebro stub y el contrato del turno.”

**Misión:** [M1-C](../aprendizaje/missions/sprint-1/M1-C-inferturn-stub.md)

**Prompt Cursor:**

```text
Somos el módulo C (inteligencia/).
Objetivo Sprint 1:
1) Arrancar python servidor_simulado.py
2) Ampliar APRENDIZAJE.md: sección "Contrato del turno (InferTurn)" en español simple
   (qué recibe, qué devuelve, por qué solo B nos llama)
3) Crear inteligencia/PRUEBA_STUB.md con pasos para probar el stub
   (health y/o ejemplo de POST documentado; token interno del .env.example)
4) Carpetas stt/, llm/, tts/, memory/ listadas (vacías OK)
NO instales Ollama/Whisper/Kokoro todavía.
Explica en 5 líneas como a un compañero de 15 años.
```

---

## Ficha D — Expresión

**Dile:** “Hoy A ya no adivina morphs: tú se los dejas en tabla.”

**Misión:** [M1-D](../aprendizaje/missions/sprint-1/M1-D-tabla-morphs.md)

**Prompt Cursor:**

```text
Somos el módulo D (expresion/).
Objetivo Sprint 1:
1) Ampliar APRENDIZAJE.md con TABLA:
   codigo (visema/gesture) | significado | morph Unity sugerido | ¿A lo tiene? (sí/no/pendiente)
2) Basarte en contratos/expresion/v1/enums.md y el fixture
3) Sección "Cómo lo usará A en el turno"
4) 10 minutos con A para llenar la columna sí/no
Sin programar Unity (A lo hará después).
Explica la tabla en 5 líneas.
```

---

# BLOQUE 4 — Trabajo individual (~60 min)

Tú das vueltas cada ~12–15 min. Preguntas:

- “¿Qué dice tu checklist?”  
- “¿Ya puedes demostrar el éxito de la tabla?”  

Quien termine temprano: bonus al final de su misión M1.

---

# BLOQUE 5 — Parejas (~35 min)

### 5.1 A ↔ B (15–20 min) — prioritario

En PC con servidor arriba:

1. A hace login real  
2. Anotan OK / error / siguiente paso en un txt o APRENDIZAJE  

### 5.2 A ↔ D (10 min)

Llenan columna “¿A lo tiene?” de la tabla morphs.

### 5.3 B ↔ C (5–10 min)

C muestra stub; B confirma humo con `INTEL_STUB=true` sigue OK.

---

# BLOQUE 6 — Cierre demo (20 min)

Cada uno ≤ 2 min:

| Rol | Muestra |
|-----|---------|
| B | README Para A + health |
| A | Login OK + token (o error amigable) |
| C | 1 min InferTurn + stub vivo |
| D | Tabla morphs |

Mentor:

> “EmpathIA ya no es solo carpetas: hay login, contrato de turno y cara planificada.  
> Próxima clase: turno con audio (A+B) y/o boca básica (A+D).”

Checklist integración Sprint 1 al final.

---

## Si alguien viene muy atrasado

Prioridad estricta:

1. Cerrar Sprint 0 mínimo  
2. Puente A↔B (login)  
3. Resto es bonus  

---

## Qué NO hacer en clase 2

- Ollama / Whisper / Kokoro  
- Cambiar `contratos/`  
- Que A hable con C  
- Lip-sync “perfecto”  
- Multi-usuario  

---

## Documentos clave

| Uso | Path |
|-----|------|
| Este guion | `documentacion/equipo/clase-2.md` |
| Sprint 1 | `documentacion/aprendizaje/missions/sprint-1/` |
| Checklists | `documentacion/aprendizaje/checklists/sprint-1/` |
| Mapa | `documentacion/aprendizaje/PROJECT_MAP.md` |
