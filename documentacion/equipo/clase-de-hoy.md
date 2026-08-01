# Clase de hoy — EmpathIA (3 h 30 min)

**Meta del día:**  
1) Entender el proyecto en grande  
2) Instalar lo mínimo en **cada PC**  
3) Asignar roles y arrancar la misión 1 con Cursor  

**Formato:** grupal → instalación por equipos → 1:1 por rol → trabajo guiado → cierre  

---

## Reloj sugerido (3 h 30)

| Bloque | Tiempo | Qué |
|--------|--------|-----|
| 0. Preparación tuya | (antes) | Servidor + prueba de humo listos en el PC demo |
| 1. Grupal | **0:00–0:35** | Idea, metáfora, roles, demo |
| 2. Instalación | **0:35–2:05** | ~90 min: setup en cada máquina |
| 3. 1:1 + arranque | **2:05–2:55** | 4× ~12 min + que peguen prompts |
| 4. Trabajo acompañado | **2:55–3:20** | Tú das vueltas; ellos producen |
| 5. Cierre | **3:20–3:30** | Qué logró cada uno / tarea para casa o mañana |

Si la instalación se atrasa, **recorta el trabajo acompañado**, no el cierre.

Si van muy rápido (instalación < 45 min), pasa al final a **“Bonus de hoy”** o abre [`clase-de-manana.md`](./clase-de-manana.md) bloque 1.

---

## Parte 0 — Tú solo (antes de clase)

- [ ] PC demo con repo en `C:\laragon\www\Emphatia`  
- [ ] Servidor:
  ```powershell
  cd C:\laragon\www\Emphatia\servidor
  php artisan serve --host=127.0.0.1 --port=8000
  ```
- [ ] Probar: `.\herramientas\prueba-humo-fase0.ps1` → `PHASE 0 SMOKE OK`  
- [ ] USB / carpeta compartida / Git listo para clonar el repo  
- [ ] Hojas de rol: `documentacion/equipo/hojas-rol/`  
- [ ] Lista de software por rol (abajo) en la pizarra  

---

# BLOQUE 1 — Todos juntos (35 min)

## 1.1 Bienvenida (5 min)

> “Tenemos 3 horas y media.  
> Hoy: entender EmpathIA, instalar herramientas en sus PCs, y cada uno empieza **una misión**.  
> Es un **prototipo**, no un producto de empresa.  
> Programamos con **Cursor** (vibe coding).”

## 1.2 Metáfora (10 min)

```text
Estudiante habla
      ↓
 [A] Avatar (Unity)     → cara, mic, boca
      ↓
 [B] Servidor (Laravel) → organiza
      ↓
 [C] Inteligencia       → entiende y responde
      ↓
 vuelve a [A]
      ↑
 [D] Expresión          → cómo se mueve la cara
```

Regla de oro (que la repitan):

> “Cada quien su carpeta. No tocamos la del compañero.”

| Rol | Carpeta |
|-----|---------|
| A Avatar | `cliente-unity/` |
| B Servidor | `servidor/` |
| C Inteligencia | `inteligencia/` |
| D Expresión | `expresion/` |

## 1.3 Asignar roles (5–8 min)

Anota nombres:

| Rol | Nombre |
|-----|--------|
| A | |
| B | |
| C | |
| D | |

Entrega **solo su hoja** de `hojas-rol/`.

## 1.4 Demo prueba de humo (10 min)

Corre el script. Al ver `PHASE 0 SMOKE OK`:

> “Sin Unity ya hay esqueleto. Hoy instalamos y cada quien empieza su pedazo.”

## 1.5 Cómo trabajamos (5 min)

1. Cursor  
2. Prompt del día  
3. “Explícame en 5 líneas, en español”  
4. Error → pegar a Cursor  
5. 3 bullets al final: hice / no entendí / necesito  

---

# BLOQUE 2 — Instalación en cada equipo (~90 min)

**Objetivo:** que cada PC pueda abrir el repo y su herramienta principal.  
No instalen “todo el stack mundial”. Solo lo de su rol + lo común.

## 2.1 Común a los 4 (hacer primero, juntos, ~35–45 min)

En **cada PC**:

| # | Qué | Para qué |
|---|-----|----------|
| 1 | Git | Clonar / actualizar repo |
| 2 | Cursor | Vibe coding |
| 3 | Clonar o copiar el repo EmpathIA | Todos ven las mismas carpetas |
| 4 | Abrir la carpeta del monorepo en Cursor | Misma vista |

**Mensaje clave:**

> “Si algo de la instalación falla, anótenlo. No se queden 40 minutos solos: levantan la mano.”

Checklist común (márcalo por PC):

| PC / estudiante | Git | Cursor | Repo abierto | OK |
|-----------------|-----|--------|--------------|-----|
| A | | | | |
| B | | | | |
| C | | | | |
| D | | | | |

### Cómo traer el repo (elige una)

**Opción fácil hoy:** copiar la carpeta `Emphatia` por USB/red al mismo path si se puede:  
`C:\laragon\www\Emphatia` (B necesita Laragon; los demás pueden usar otra ruta, ej. `C:\EmpathIA`).

**Opción mejor:** `git clone` cuando tengan remoto. Si aún no hay GitHub, USB está bien **hoy**.

## 2.2 Instalación por rol (~45 min, en paralelo)

Tú das vueltas. Cada uno instala **solo lo suyo**:

### A — Avatar

- [ ] Unity Hub  
- [ ] Unity **6** (o la versión que acuerden; si tarda mucho, deja descargando y sigue con README)  
- [ ] Abrir carpeta `cliente-unity/` en Cursor  

**Si Unity pesa mucho:** que deje la descarga y en la misión 1 haga README + diseño de pantalla login (no los bloquees todo el día).

### B — Servidor

- [ ] Laragon (PHP + Composer ya vienen)  
- [ ] Copiar/clonar repo bajo `C:\laragon\www\Emphatia`  
- [ ] En terminal:
  ```powershell
  cd C:\laragon\www\Emphatia\servidor
  php artisan serve --host=127.0.0.1 --port=8000
  ```
- [ ] Probar prueba de humo en su PC (ideal) o al menos abrir `http://127.0.0.1:8000/api/v1/health`  

**Si Composer/vendor falta:** desde `servidor/` → `composer install`

### C — Inteligencia

- [ ] Python 3 (PATH OK: `python --version`)  
- [ ] Probar:
  ```powershell
  cd C:\laragon\www\Emphatia\inteligencia
  python servidor_simulado.py
  ```
- [ ] Abrir `http://127.0.0.1:8100/internal/v1/health` (si pide token, con que el proceso arranque basta hoy)

### D — Expresión

- [ ] Cursor + repo (ya del común)  
- [ ] Abrir `expresion/` y `contratos/expresion/`  
- [ ] (Opcional) extensión JSON en Cursor  
- No necesita Unity hoy, pero sí hablará con A mañana  

## 2.3 Mini-validación de instalación (10 min, todos)

Cada uno dice: “En mi PC ya puedo ___”.

- A: “Abrir Unity Hub / o dejarlo descargando + Cursor”  
- B: “Ver health OK o artisan serve”  
- C: “Correr servidor_simulado”  
- D: “Ver el JSON de ejemplo en Cursor”  

---

# BLOQUE 3 — 1:1 por rol (50 min)

Orden: **B → A → C → D** (~12 min c/u).

Mientras esperan: leen su hoja; **no empiezan código** hasta su 1:1.

### En cada 1:1

1. “¿Tu rol en una frase?”  
2. Confirmas con la ficha  
3. Le pegas el **prompt del día**  
4. Lo ves enviar el primer mensaje a Cursor  
5. “Tu éxito de hoy es…”  

---

## Ficha B — Servidor

> “Tú organizas. Todo pasa por `servidor/`. Unity no habla con la IA directo.”

**Éxito hoy:** `servidor/README.md` con 5 curls en español.

```text
Somos el módulo B de EmpathIA (Laravel en la carpeta servidor/).
NO reescribas la arquitectura.
Objetivo de hoy: ampliar servidor/README.md con 5 ejemplos curl para Windows:
1) login
2) crear sesión
3) subir turno (audio)
4) ver events
5) cerrar sesión
Usuario: estudiante1
Contraseña: password
Base: http://127.0.0.1:8000/api/v1
Texto en español claro para bachillerato.
Al final explícame en 5 líneas qué agregaste.
```

---

## Ficha A — Avatar

> “Tú eres la cara: `cliente-unity/`. Solo hablas con el servidor.”

**Éxito hoy:** escena login “Login OK” **o** (si Unity descarga) README + boceto de pantalla.

```text
Somos el módulo A de EmpathIA (Unity 6). Carpeta: cliente-unity/
NO toques servidor/ ni inteligencia/.
Objetivo de hoy:
1) Crear o abrir proyecto Unity 6 en cliente-unity/ (si Unity aún descarga, prepara README y describe la escena)
2) Escena simple: usuario, contraseña, botón Entrar
3) Al entrar, guarda un token (texto) y muestra "Login OK"
4) README en español: qué falta para conectar
   http://127.0.0.1:8000/api/v1/auth/login
Explica cada archivo nuevo en español, muy simple.
```

---

## Ficha C — Inteligencia

> “Tú piensas después. Hoy solo el simulador en `inteligencia/`.”

**Éxito hoy:** `inteligencia/APRENDIZAJE.md` + carpetas `stt/`, `llm/`, `tts/`, `memory/`.

```text
Somos el módulo C de EmpathIA (carpeta inteligencia/).
Lee servidor_simulado.py y el README.
Objetivo de hoy:
1) Crear inteligencia/APRENDIZAJE.md en español simple explicando el simulador
2) Carpetas vacías stt/, llm/, tts/, memory/ (con .gitkeep si hace falta)
3) En APRENDIZAJE.md: qué irá en cada carpeta más adelante
No conectes Ollama ni APIs externas todavía.
Al final explícame en 5 líneas qué hiciste.
```

---

## Ficha D — Expresión

> “Tú defines cómo se mueve la cara. Hoy documentas; no programas Unity.”

**Éxito hoy:** `expresion/APRENDIZAJE.md` con visemas + lista de morphs para A.

```text
Somos el módulo D de EmpathIA.
Lee expresion/fixtures/paquete-expresion-ejemplo.json
y contratos/expresion/v1/enums.md
Crea expresion/APRENDIZAJE.md en español simple con:
1) qué es un visema (ejemplo)
2) qué hace cada gesture del archivo de ejemplo
3) lista de morphs que pediremos al rol A (Unity)
Sin código de Unity todavía.
Al final explícame en 5 líneas qué escribiste.
```

---

# BLOQUE 4 — Trabajo acompañado (25 min)

Tú das vueltas (5–6 min por persona). Pregunta solo:

- “¿Qué te respondió Cursor?”  
- “¿Qué no entiendes de esas 5 líneas?”  
- “Muéstrame el archivo que estás creando”  

**Prohibido:** meterse a arreglar código 20 min en un solo PC y abandonar a los otros.

---

# BLOQUE 5 — Cierre (10 min)

Cada uno, 20 segundos:

1. Mi rol es…  
2. Hoy instalé… / logré…  
3. Me falta…  

Tú:

> “Mañana: conectamos piezas entre roles (sin inventar arquitectura nueva).  
> Traigan PC con lo instalado y sus 3 bullets.”

Si terminaron misiones temprano → abrir [`clase-de-manana.md`](./clase-de-manana.md).

---

## Bonus de hoy (solo si sobra tiempo)

| Rol | Extra corto |
|-----|-------------|
| B | Probar uno de los curls de verdad en su PC |
| A | Dibujar estados: escuchando / pensando / hablando |
| C | Pegar en APRENDIZAJE captura o texto del health del simulador |
| D | 5 min con A: “¿qué morphs podría tener el avatar?” |

---

## Qué NO hacer hoy

- Instalar Ollama/Whisper/Kokoro  
- Pedir lip-sync perfecto  
- Repartir backlog fase 1 completo  
- Que alguien “ayude” entrando a carpeta ajena  

---

## Checklist final del día

- [ ] 4 roles con nombre  
- [ ] 4 PCs con Cursor + repo  
- [ ] B: Laragon/artisan o al menos README avanzado  
- [ ] A: Unity en marcha o plan + README  
- [ ] C: simulador corre + APRENDIZAJE  
- [ ] D: APRENDIZAJE  
- [ ] Saben que mañana hay clase 2 lista  
