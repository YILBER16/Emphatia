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
