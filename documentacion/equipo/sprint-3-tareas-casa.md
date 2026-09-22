# Sprint 3 — Tareas para casa (trabajo virtual)

Cada uno trabaja **en su PC**. No esperen al laboratorio.

Tiempo: **2 a 3 horas** esta semana (pueden partirlo en dos noches).  
Entrega: **antes de la próxima clase**. Mandan evidencia por el grupo (foto, video corto o mensaje).

Si se traban: Cursor + “explícamelo como si no fuera programador”. Si sigue trabado, escriben en el grupo: *rol + qué intentaron + error*.

---

## 0) Todos, antes de empezar (15 min)

Hagan esto **una vez**, en este orden:

1. Abran una terminal en la carpeta del proyecto.
2. Bajen lo último:
   ```text
   git checkout main
   git pull origin main
   ```
3. Creen **su** rama (copien la de su rol, más abajo).
4. Abran Cursor **en la raíz** `Emphatia`, no en una subcarpeta.
5. Lean solo **su** hoja: `documentacion/equipo/hojas-rol/`

No toquen la carpeta de otra persona.

---

## Isaac (A) — en casa

**Rama:** `git checkout -b a/tts-morph`

**Carpeta:** `cliente-unity/`  
**Tiempo:** ~2 h 30

### Pasos

1. Abre Unity en `cliente-unity/avatar`. Dale Play. Anota si el login aparece.
2. En `cliente-unity/APRENDIZAJE.md` escribe 5 líneas: *qué voy a hacer esta semana* (oír audio + 1 boca).
3. Busca en el código `DownloadAndPlayTts`. Confirma que **después** de `turn.result` se llama y suena. Si no se llama, conéctalo (Cursor te ayuda: “cuando llegue turn.result, reproduce el TTS”).
4. Prueba en tu PC:
   - Si tienes Laragon: arranca el servidor B (`php artisan serve --host=127.0.0.1 --port=8000`) y en Unity usa `http://127.0.0.1:8000/api/v1`.
   - Login: `orientador1` / `password` → elige estudiante → manda un texto.
   - **Meta:** oír algo. Si sale silencio, no te detengas: anota “TTS llegó pero es silencio” y sigue al paso 5. Eso se lo arreglan Stid y Nikol.
5. Abre el avatar 3D. En Inspector busca **Blendshapes** / morphs. Anota 3 nombres que veas (o escribe “este modelo no tiene morphs”).
6. Haz **un** movimiento: cuando el estado sea `speaking`, sube un morph (boca / jaw / lo que exista). No busques 15 visemas.
7. Graba un video de **20–30 segundos** (celular al monitor vale): Play → se ve la respuesta y, si puedes, se oye o se mueve la boca.

### Entregas (manda al grupo)

- [ ] Video corto o 2 capturas (respuesta en pantalla + morph o el Inspector sin morphs)
- [ ] APRENDIZAJE actualizado
- [ ] Mensaje: “Isaac listo / a medias / trabado en ___”

### No hagas en casa

Llamar a Nikol a `:8100`. Pedir WebSockets. Esperar a que D termine para empezar: el morph de prueba lo puedes hacer tú.

---

## Stid (B) — en casa

**Rama:** `git checkout -b b/preferred-name`

**Carpeta:** `backend/`  
**Tiempo:** ~2 h

### Pasos

1. Arranca Laravel:
   ```text
   cd backend
   php artisan serve --host=127.0.0.1 --port=8000
   ```
2. Prueba: en el navegador `http://127.0.0.1:8000/api/v1/health` debe responder.
3. Abre `backend/app/Services/TurnOrchestrator.php`. Busca `callIntelligence` y `callIntelligenceText` (las llamadas a Nikol).
4. En esas llamadas, agrega `preferred_name` con el **nombre de preferencia** del perfil del estudiante. Solo eso. Nada de cédula ni teléfono.
   - Cursor: “en InferTurn manda preferred_name del StudentProfile, sin otros datos personales”.
5. Prueba con PowerShell (tú en tu PC; no hace falta Isaac):
   - Login `admin1` / `password`
   - O usa el script `herramientas/demo-perfiles-estudiante.ps1` y un turno de texto
   - Revisa el log: debe verse que sale el nombre (ej. Anita)
6. En `backend/README.md` agrega un párrafo corto: qué es `INTEL_STUB`, cómo ponerlo en `true` (simulado) y `false` (llama a Nikol). Palabras simples.
7. Si puedes instalar Python: arranca `inteligencia/servidor_simulado.py`, pon `INTEL_STUB=false` en `.env`, y haz **un** turno de texto. Si no puedes, déjalo listo y en el Meet de 20 min con Nikol lo prueban.

### Entregas

- [ ] Captura del health OK
- [ ] Captura o log donde se ve `preferred_name`
- [ ] Párrafo nuevo en el README
- [ ] Mensaje: “Stid listo / a medias / trabado en ___”

### No hagas en casa

WebSockets, MySQL, tocar Unity, tocar `inteligencia/` (salvo arrancar el simulador para probar).

---

## Nikol (C) — en casa

**Rama:** `git checkout -b c/stt-wav`

**Carpeta:** `inteligencia/`  
**Tiempo:** ~2 h 30

### Pasos

1. Arranca tu servicio:
   ```text
   cd inteligencia
   python servidor_simulado.py
   ```
2. Crea (si no existen) las carpetas `inteligencia/stt`, `llm`, `tts`, `memory`. En cada una un `README.md` de 5 líneas: “aquí irá …”.
3. Pon un WAV de prueba en `datos/` (cualquier audio corto en español, o grábate 5 segundos con el celular y pásalo a WAV).
4. Llama a InferTurn con ese archivo (`audio.path` = ruta completa en **tu** PC). Sigue `inteligencia/PRUEBA_STUB.md` y agrega este caso al final.
5. El JSON debe traer `transcript` (texto). Si Whisper no corre, el plan B está bien: **escribe en el log** “usé fallback”.
6. El campo `tts.path` debe ser un archivo **que se oiga**. Un beep o una frase. Silencio = no cuenta. Cursor: “genera un wav corto con un tono y guárdalo en tts.path”.
7. Reproduce ese WAV en el reproductor de Windows. Graba 10 segundos con el celular.
8. En `inteligencia/APRENDIZAJE.md` agrega **Plan en casa**:
   - Hoy: transcribo audio y suelto un wav que se oye
   - Después: TTS más real (Kokoro o similar)
   - No cambio la forma del JSON

### Entregas

- [ ] Video o audio del WAV de salida
- [ ] Pedazo de JSON (transcript + tts.path)
- [ ] APRENDIZAJE con el plan
- [ ] Mensaje: “Nikol listo / a medias / trabado en ___”

### No hagas en casa

Login del colegio. Escribir en la base de Stid. Ollama “por probar”. Cambiar contratos.

---

## Expresión (D) — en casa

Si **no hay persona D**, Isaac hace los pasos 1–4 y lo dice en el grupo.

**Rama:** `git checkout -b d/tabla-morphs`

**Carpeta:** `expresion/`  
**Tiempo:** ~1 h 30 (+ 15 min Meet con Isaac)

### Pasos (solo, sin Unity)

1. Abre `expresion/fixtures/paquete-expresion-ejemplo.json`.
2. Abre `contratos/expresion/v1/enums.md` (lista de bocas: `aa`, `O`, `PP`…).
3. Crea `expresion/APRENDIZAJE.md` en español de salón: qué es un visema (forma de boca), qué es un gesto, qué **no** haces tú.
4. Crea `expresion/TABLA_MORPHS.md` con una tabla:

   | Código | Qué boca es | ¿Isaac lo tiene? |
   |--------|-------------|------------------|
   | sil | cerrada | ? |
   | aa | A | ? |
   | … mínimo 5 … | | |
   | soft_smile | sonrisa suave | ? |
   | soft_concern | preocupación | ? |

5. Debajo, 3 pasos: “Cuando el avatar está hablando, Isaac debe: 1) … 2) … 3) …”.
6. Manda la tabla al grupo y pide a Isaac 15 min por Meet para llenar la columna “¿lo tiene?”. Si el modelo no tiene morphs, escriben “no” en todas y **también vale**.

### Entregas

- [ ] `APRENDIZAJE.md`
- [ ] `TABLA_MORPHS.md`
- [ ] Mensaje: “D listo / a medias / trabado en ___”

---

## Meet corto (virtual, 20–30 min)

Cuando **cada uno** haya hecho lo de casa, un Meet. No es para programar desde cero.

| Min | Quién | Qué |
|-----|-------|-----|
| 0–5 | Todos | “¿listo / a medias / trabado?” |
| 5–15 | Stid + Nikol | Un turno de texto con simulador apagado, en **un** PC (comparten pantalla). El audio entre dos casas distintas se rompe; por eso texto primero. |
| 15–25 | Isaac + D | Columna de morphs + 1 movimiento en Play |
| 25–30 | Todos | Qué falta para la clase |

Audio de voz **entre PCs distintos** no es tarea de casa. Eso se deja para cuando estén en el mismo computador, o Stid y Nikol en una sola máquina compartiendo pantalla.

---

## Cómo saber si ya terminé

| Rol | Ya terminé en casa si… |
|-----|-------------------------|
| Isaac | Video: se ve la respuesta y hay 1 intento de boca **o** foto del Inspector sin morphs |
| Stid | Log/captura con `preferred_name` + párrafo INTEL_STUB |
| Nikol | Se oye un WAV de salida + JSON de transcripción |
| D | Tabla + 3 pasos escritos |

Nadie tiene que terminar el circuito completo solo. En casa adelantan **su** pieza. En el Meet las juntan.
