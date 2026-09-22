# Sprint 3 — Mensajes para el equipo (copiar y pegar)

Texto listo para WhatsApp, Classroom o Cursor. Lenguaje de clase, no de manual.

Las misiones técnicas siguen en `documentacion/aprendizaje/missions/sprint-3/`.

---

## Al grupo

```text
Hola equipo EmpathIA

Ya tenemos login, lista de estudiantes y que el avatar muestre una respuesta en texto. Bien.

Esta semana no inventamos cosas nuevas raras. Cerramos el circuito para que se SIENTE el prototipo:

1. Que se OIGA la voz de respuesta
2. Que la cara mueva UN poquito la boca
3. Que la IA reciba el nombre con el que el niño quiere que lo llamen

Cada uno tiene UNA misión. No tienen que entender el repo entero.

Reglas de siempre:
- Cada quien su carpeta
- Si no saben, preguntan
- El viernes mostramos en el proyector: se oye algo + se mueve algo

Isaac = avatar (Unity)
Stid = servidor
Nikol = inteligencia
Expresión = la cara (si no hay D, Isaac y el profe lo hacen en 20 min)

Pull primero: git pull origin main
```

---

## Isaac (Rol A — el actor)

```text
Isaac, tú eres lo que el niño VE.

Ya lograste: entrar como adulto, elegir estudiante, enviar texto/audio y ver la respuesta escrita. Eso ya es Sprint 2.

Esta semana (Sprint 3) son dos cosas, no veinte:

1. Que se OIGA la respuesta
   Cuando llegue el resultado, Unity tiene que reproducir el audio. Si no se oye, no está listo. Si falla, avísale a Stid (servidor) o a Nikol (el archivo de voz).

2. Que se mueva UN solo gesto de boca
   No lip-sync de película. Un morph: boca un poco abierta mientras “habla”. Eso lo acuerdas con D (o con el profe si no hay D).

Importante:
- Tú NO le hablas a la IA directo. Solo al servidor de Stid.
- El dictado de Windows que hiciste es un plan B de laboratorio. El diseño de verdad es: el audio se va al servidor y Nikol transcribe.

Carpeta: cliente-unity/
Rama sugerida: a/tts-morph
Listo cuando: en Play se oye algo y se ve 1 movimiento de boca (o dejas escrito “el avatar no tiene morphs”).

Si te trabas en la API → Stid
Si te trabas en la boca → D / profe
```

---

## Stid (Rol B — la recepción)

```text
Stid, tú eres la recepción. Nadie habla con nadie si no pasa por ti.

Ya lograste: login, turnos, perfiles de estudiante sin contraseña del niño, y conectar a Nikol.

Esta semana (Sprint 3) son dos cosas:

1. Mandarle a Nikol SOLO el nombre preferido
   Ejemplo: “Anita”. No mandes cédula, teléfono ni nombre completo. Eso ya está en el perfil; hay que ponerlo en la llamada a la IA.

2. Apagar el modo “simulado” una vez y demostrar que Nikol responde de verdad
   Eso es INTEL_STUB=false (el interruptor que dice “no finjas, llama a C”).
   Tienen que estar tú y Nikol en EL MISMO computador del piloto. Si cada uno está en su casa con rutas distintas, el audio se pierde.

También: el link del audio de respuesta tiene que servir un archivo que SE OIGA, no un silencio vacío.

Carpeta: backend/ (en docs a veces dice servidor/)
No hagas esta semana: WebSockets, MySQL, caras del avatar.

Listo cuando: hay un log de B→C con el nombre, un turno con stub apagado, y el WAV se puede descargar.

Si C no contesta → Nikol
Si Unity no conecta → Isaac
```

---

## Nikol (Rol C — quien piensa)

```text
Nikol, tú piensas y contestas. Unity no te llama a ti. Te llama Stid.

Ya lograste: el simulador, Gemini, los prompts por emoción y el nombre preferido limpio. Vas adelantada. Esta semana bajamos a lo que el equipo NECESITA oír.

Esta semana (Sprint 3):

1. Si llega un audio, sácale el texto
   Whisper está bien. Si falla, di “usé el plan B” en el log. No cambies la forma del JSON.

2. Devuelve un archivo de voz que SE OIGA
   Un beep, una frase grabada, lo que sea. El silencio de ahora no cuenta. Isaac necesita oír algo.

3. En APRENDIZAJE escribe 10 líneas:
   Hoy hago X. Después (Sprint 4) haré TTS más real. Así no se pierde el plan.

Carpetas vacías stt / llm / tts / memory: créalas aunque tengan un README de 5 líneas. Es para no mezclar todo en un solo archivo después.

Carpeta: inteligencia/
No hagas: login del colegio, meter datos en la base de Stid, Ollama “porque sí”.

Listo cuando: un WAV de prueba entra, sale texto + un WAV que se oye, y Stid lo prueba con el interruptor simulado apagado.

Si el audio no aparece en tu PC → Stid (tienen que estar en la misma máquina)
```

---

## Quien haga D (coach de la cara)

```text
Tú no programas la IA ni el servidor. Tú dices cómo se mueve la cara.

Hasta ahora esa parte casi no se tocó. Esta semana es ponernos al día, fácil:

1. Lee el JSON de ejemplo de la cara (el “paquete de expresión”).
2. Haz una tabla: visema (forma de boca) → morph de Unity. Mínimo 5 bocas y 2 gestos. Isaac te dice cuáles SÍ tiene el avatar.
3. Escribe 3 pasos: “Cuando esté hablando, Isaac debe…”
4. En 15 minutos con Isaac, mueven UN morph. Con eso basta.
   Si el avatar no tiene morphs, lo escriben: “no se puede, el modelo no trae boca”. Eso también cuenta.

Carpeta: expresion/
No cambies contratos sin avisar.
No pidas cine. El éxito es: se nota que habla.

Si no hay persona D: Isaac + profe hacen la tabla y el morph en 20 min.
```

---

## Cómo preguntarle a Cursor (ellos)

```text
Explícamelo como si no fuera programador.
Soy el rol X. ¿Qué hago HOY en UNA frase?
Si me trabo, dime a quién le pregunto.
```

---

## Tareas de casa (virtual) — al grupo

Pasos detallados: `documentacion/equipo/sprint-3-tareas-casa.md`

```text
Equipo: esta semana es VIRTUAL. Cada uno en su casa, 2-3 horas, antes de la próxima clase.

1) git checkout main
2) git pull origin main
3) Crean SU rama y trabajan SOLO su carpeta

Isaac rama a/tts-morph
Stid rama b/preferred-name
Nikol rama c/stt-wav
Expresión rama d/tabla-morphs

Al terminar mandan al grupo: "listo / a medias / trabado en ___" + captura o video de 20s.

No esperen al lab para empezar. El circuito completo (audio entre dos PCs) NO se pide en casa. Cada quien adelanta su pieza.

Isaac: oír TTS si puedes + 1 morph (o foto de que el avatar no tiene boca)
Stid: mandar preferred_name a Nikol + anotar INTEL_STUB en el README
Nikol: un WAV que se OIGA + transcribir un audio de prueba en TU PC
D: tabla de bocas + 3 pasos para Isaac

Meet 20 min cuando hayan adelantado: Stid+Nikol (texto) e Isaac+D (boca).
```

