# APRENDIZAJE — Sprint 0 (Rol C Inteligencia)

## 1) Que hace el simulador

El archivo `servidor_simulado.py` levanta un servicio local en `127.0.0.1:8100`.
Este servicio simula la IA real y expone endpoints internos para que el servidor B pueda probar el flujo completo sin instalar Whisper, Ollama ni Kokoro.

Endpoints del simulador:

- `GET /internal/v1/health`
- `POST /internal/v1/infer/turn`
- `POST /internal/v1/memory/prepare`
- `POST /internal/v1/memory/purge`

Seguridad interna:

- El simulador valida el header `X-Internal-Token`.
- Solo el servidor B debe llamar a estos endpoints.

## 2) Que es InferTurn

InferTurn es la operacion principal del modulo C.
Recibe una peticion con audio y metadatos de la sesion del estudiante, y devuelve una respuesta estructurada para que B guarde eventos y para que D anime el avatar.

### Entrada (request)

Campos clave del contrato `infer-turn.request.schema.json`:

- `request_id` (uuid)
- `session_id` (uuid)
- `turn_id` (uuid)
- `student_id` (uuid)
- `locale` (debe ser `es`)
- `audio.path` (ruta del audio)
- `options` (opcional, por ejemplo latencia maxima)

### Salida (response)

Campos clave del contrato `infer-turn.response.schema.json`:

- `request_id`
- `transcript` (texto y confianza)
- `emotion` (etiqueta y confianza)
- `risk_signals` (codigo, severidad, evidencia, confianza)
- `reply` (texto empatico y flags de guardrail)
- `tts` (ruta de audio, formato wav, duracion)
- `timing` (calidad y cues para visemas)
- `memory` (si actualizo memoria)
- `model_versions`
- `metrics` (incluye `total_ms`)

Resumen rapido: audio entra, C infiere y devuelve texto, emocion, riesgo, respuesta y audio con timing.

## 3) Que NO escribe C en la DB de B

El Rol C no debe escribir en la base de datos de Laravel del servidor B.
C solo realiza inferencia y devuelve JSON por API interna.

No hace:

- Inserciones en tablas de B.
- Login/autorizacion institucional.
- Persistencia de negocio de sesiones/turnos en Laravel.

Si hay que guardar algo institucional, lo hace B despues de recibir la respuesta de C.

## 4) Carpetas futuras del pipeline

Se dejan creadas para la siguiente fase:

- `inteligencia/stt/`
- `inteligencia/llm/`
- `inteligencia/tts/`
- `inteligencia/memory/`

## 5) Evidencia Sprint 0

Checklist de evidencia minima:

1. Simulador ejecutando sin traceback.
2. Health responde `status: ok`.
3. Contratos de `contratos/inteligencia/v1/` revisados sin modificarlos.
4. APRENDIZAJE explicado en menos de 1 minuto.

## 6) Contrato del turno (InferTurn)

En Sprint 1 y Sprint 2 el foco no es IA real todavia: es explicar y demostrar el contrato que usa el turno con audio.

### Entrada

- `session_id`
- `turn_id`
- `student_id`
- `locale` con valor `es`
- `audio.path` apuntando a un WAV local o generado por el flujo de B

### Salida

- `transcript`
- `emotion`
- `risk_signals`
- `reply`
- `tts`
- `timing`
- `memory`
- `model_versions`
- `metrics`

### Regla de trabajo

- C no escribe en la DB de B.
- B llama a C por API interna.
- El header interno es `X-Internal-Token`.
- Si aun no hay motor real, se documenta el stub y se mantiene el mismo contrato.

## 7) Fase 2 — Biblioteca de prompts por escenario

La biblioteca local vive en `inteligencia/prompts/` y se versiona junto con el
servidor. `registry.json` identifica el prompt activo para cada escenario y
cada plantilla contiene objetivo, tono, estrategia y limites de seguridad.

Escenarios disponibles:

- Emociones: tristeza, ansiedad, cansancio, frustracion, soledad, miedo,
	enojo, culpa y verguenza.
- Situaciones: presion por examenes, bullying, conflicto familiar y estudiante
	que no quiere hablar.
- Riesgo: bajo, medio, alto y emergencia.

Para seleccionar una plantilla, B puede enviar estos campos opcionales:

```json
{
	"emotion": {"label": "ansiedad"},
	"risk_level": "low"
}
```

La prioridad de seleccion es:

1. `emergency` o `emergencia` usa `emergencia-v1`.
2. `high`, `critical` o `immediate` usa `riesgo-alto-v1`.
3. `medium` o `moderate` usa `riesgo-medio-v1`.
4. Si no hay riesgo priorizado, se selecciona la emocion reconocida.
5. Si no hay coincidencia, se usa `general-v1`.

La respuesta registra el prompt utilizado en `model_versions.prompt`. Esto
permite auditar y comparar respuestas sin cambiar el contrato de `InferTurn`.

## 8) Fase 3 — Personalizacion segura

B puede enviar opcionalmente un nombre preferido separado de los datos
personales restantes:

```json
{
	"preferred_name": "Sofia"
}
```

C acepta nombres de hasta 40 caracteres, de una o dos palabras, y permite
acentos, guiones y apostrofes. Valores con etiquetas, instrucciones, exceso de
palabras o caracteres no validos se descartan antes de construir el prompt.

Gemini recibe solo el nombre preferido validado y una instruccion para usarlo
con naturalidad, sin repetirlo en cada respuesta ni inventar apodos. El nombre
completo, correo, telefono y demas datos del pre-registro no se envian a
Gemini.

El campo `preferred_name` es opcional en
`contratos/inteligencia/v1/infer-turn.request.schema.json`. Las reglas de
sanitizacion se cubren en `inteligencia/test_fase3_personalizacion.py`.
