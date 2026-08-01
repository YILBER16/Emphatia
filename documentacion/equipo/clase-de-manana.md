# Clase de mañana — EmpathIA (Sesión 2)

**Usar si:** terminan temprano la clase de hoy **o** como plan del día siguiente.  
**Duración flexible:** 2 h (mínimo) · **3 h 30** (completo, igual que hoy).  
**Meta:** conectar roles entre sí (sin IA real ni lip-sync perfecto) + avanzar misión 2.

**Premisa:** ayer instalaron (o dejaron descargas). Hoy **validamos setup** y hacemos el primer “puente” entre compañeros.

---

## Reloj 3 h 30 (completo)

| Bloque | Tiempo | Qué |
|--------|--------|-----|
| 1. Arranque + checklist setup | **0:00–0:25** | ¿Qué quedó instalado? |
| 2. Repaso 5 min + meta del día | **0:25–0:35** | Metáfora otra vez |
| 3. Misión 2 grupal (puentes) | **0:35–1:05** | Explicas los 4 puentes |
| 4. 1:1 cortos | **1:05–1:45** | 4× ~10 min, prompts misión 2 |
| 5. Trabajo + integración | **1:45–3:10** | Parejas 10 min A↔B y A↔D |
| 6. Cierre demo | **3:10–3:30** | Cada uno muestra 1 cosa |

**Si solo tienes 2 horas:** bloques 1–4 + cierre corto (omite parejas largas).

**Si vienen de “bonus de hoy” el mismo día:** salta al bloque 3.

---

## Parte 0 — Tú solo (antes)

- [ ] Servidor demo arriba + prueba de humo OK  
- [ ] Saber quién terminó / quién quedó a medias ayer  
- [ ] Tener prompts misión 2 listos (abajo)  

---

# BLOQUE 1 — Checklist de instalación (25 min)

En pizarra, columna por estudiante:

| | A | B | C | D |
|--|---|---|---|---|
| Cursor + repo | | | | |
| Herramienta de rol OK | Unity? | artisan/health? | python simulador? | JSON abierto? |
| Misión 1 entregada (archivo/escena) | | | | |

**Regla:** quien no terminó setup, primeros 20 min solo eso (tú ayudas). Quien ya está OK, relee su `APRENDIZAJE` / README y prepara 3 bullets de ayer.

---

# BLOQUE 2 — Repaso (10 min)

Misma metáfora. Pregunta rápida:

> “Si el avatar habla con la IA directo, ¿quién queda fuera?” → Respuesta: el servidor (B). Malo.

Meta de mañana:

> “Hoy no inventamos módulos nuevos.  
> Hoy **conectamos**: A habla el idioma de B; D deja lista clara a A; C documenta el contrato del simulador; B deja curls que A pueda copiar.”

---

# BLOQUE 3 — Los 4 puentes (30 min, todos)

Explica en la pizarra **una flecha por pareja**:

```text
A  ←—— curls / login ——→  B
A  ←—— lista morphs ——→  D
B  ←—— health simulador →  C
C  ←—— “qué devolverá el turno” (texto del contrato, sin código Unity)
```

### Puente 1 — A ↔ B (el más importante mañana)

B ya tiene (o termina) curls.  
A intenta **login real** contra `http://127.0.0.1:8000` (en el PC de B o en el mismo PC si B corre el servidor).

> “Éxito: A ve un token de verdad, no inventado.”

### Puente 2 — A ↔ D

D trae lista de morphs.  
A marca: ¿el avatar los tiene? sí/no/después.

> “Éxito: lista compartida de 5–10 formas de boca/cara.”

### Puente 3 — B ↔ C

B corre con `INTEL_STUB=true` (sigue OK).  
C demuestra simulador arriba.  
B y C abren juntos `contratos/inteligencia/` y leen en voz alta qué entra/sale (sin implementar).

> “Éxito: C explica en 1 minuto qué es un ‘turno’ para la IA.”

### Puente 4 — Todos

Acuerdan **una sola máquina “piloto”** para demos (la de B o la del proyector).

---

# BLOQUE 4 — 1:1 misión 2 (~10 min c/u)

Orden: **B → A → C → D**

---

## Misión 2 — B (Servidor)

**Éxito:** A puede hacer login con un curl/doc que B escribió; health responde en su PC.

```text
Somos el módulo B (carpeta servidor/).
Objetivo de hoy (misión 2):
1) Revisar que php artisan serve funciona en este PC
2) Mejorar servidor/README.md: sección "Para el compañero A (Unity)"
   con pasos exactos: arrancar servidor, login, qué hacer con el token
3) Agregar ejemplo de respuesta JSON del login (ficticia o real copiada)
4) Probar GET http://127.0.0.1:8000/api/v1/health y anotar el resultado en el README
No implementes WebSockets ni MySQL hoy.
Explica en 5 líneas qué cambió.
```

---

## Misión 2 — A (Avatar)

**Éxito:** login contra API real de B (o mensaje de error claro si B no está arriba — y cómo reintentar).

```text
Somos el módulo A (cliente-unity/).
Objetivo de hoy (misión 2):
1) Conectar el botón Entrar al API real:
   POST http://127.0.0.1:8000/api/v1/auth/login
   body JSON: {"username":"estudiante1","password":"password"}
2) Guardar el token que devuelve el servidor
3) Mostrar en pantalla "Login OK" + los primeros 10 caracteres del token
4) Si falla la conexión, mostrar mensaje de error amigable en español
Usa el README del compañero B si existe.
NO toques inteligencia/ ni expresion/.
Explica en 5 líneas cómo quedó el flujo.
```

**Si Unity aún no está:** misma misión en un script C# vacío documentado + Postman/curl manual contigo, y UI mock.

---

## Misión 2 — C (Inteligencia)

**Éxito:** documento corto “contrato del turno” en español + simulador demo al equipo.

```text
Somos el módulo C (inteligencia/).
Objetivo de hoy (misión 2):
1) Asegura que python servidor_simulado.py arranca
2) Amplía APRENDIZAJE.md con sección "Contrato del turno" en español simple:
   - qué recibe (audio / ids)
   - qué devuelve (texto estudiante, emoción, respuesta, audio, timing)
3) Copia 5 líneas de ejemplo del JSON de respuesta (del código o inventadas fieles al simulador)
4) NO instales Ollama/Whisper todavía
Explica en 5 líneas el contrato como se lo dirías a un compañero de 15 años.
```

---

## Misión 2 — D (Expresión)

**Éxito:** tabla morphs para A + 1 fixture explicado.

```text
Somos el módulo D (expresion/).
Objetivo de hoy (misión 2):
1) Amplía APRENDIZAJE.md con una TABLA:
   visema/gesture | qué significa | morph sugerido en Unity | ¿A lo tiene? (sí/no/pendiente)
2) Revisa paquete-expresion-ejemplo.json y agrega sección "Cómo lo usará A"
3) Agenda mental: 10 minutos con A hoy para llenar la columna sí/no
Sin código Unity todavía (A lo implementa después).
Explica en 5 líneas la tabla.
```

---

# BLOQUE 5 — Trabajo + parejas (85 min en sesión larga)

### 5.1 Trabajo individual (40 min)

Cada uno avanza misión 2. Tú das vueltas.

### 5.2 Pareja A↔B (20 min)

En el PC de B (servidor arriba):

1. A intenta login desde Unity (o curl si Unity no lista)  
2. Anotan: funcionó / error / siguiente paso  

### 5.3 Pareja A↔D (15 min)

Llenan juntos la columna “¿A lo tiene?” de la tabla de morphs.

### 5.4 Mini B↔C (10 min)

C enseña el simulador 2 minutos; B confirma que con stub el smoke sigue OK.

---

# BLOQUE 6 — Cierre demo (20 min)

Cada uno muestra **una** cosa en proyector (máx 2 min):

| Rol | Muestra |
|-----|---------|
| B | README “Para el compañero A” + health |
| A | Login OK con token (o error amigable) |
| C | 1 minuto “qué es un turno” |
| D | Tabla morphs |

Tú cierras:

> “Ya no son 4 islas: hay puentes.  
> Próxima sesión: micrófono/turno (A+B) o cara básica (A+D).  
> Todavía no IA real.”

---

## Si mañana también son 3 h 30 y van lentos

Prioridad estricta:

1. Setup que faltó  
2. Puente A↔B (login real)  
3. Tabla A↔D  
4. Lo demás es bonus  

---

## Qué NO hacer mañana

- Ollama / Whisper / Kokoro  
- WebSockets reales  
- MySQL obligatorio  
- Refactorizar contratos  
- Que C o D toquen Unity  

---

## Checklist final sesión 2

- [ ] A obtuvo token real (o deja documentado el bloqueo)  
- [ ] B tiene sección README para A  
- [ ] C explicó contrato del turno  
- [ ] D tiene tabla morphs con sí/no de A  
- [ ] Una máquina piloto acordada  
