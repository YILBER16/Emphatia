# M2-C — Turno con audio y contrato vivo de InferTurn

## ID

`M2-C-turno-audio-inferturn`

## Título

Preparar el turno con audio y explicar el camino hacia IA real

## Rol

C

## Objetivo

Que el equipo pueda entender, en español simple, cómo funciona el turno de Inteligencia cuando entra audio y qué devuelve el stub; además, dejar preparada la base documental para reemplazarlo por STT, LLM y TTS reales sin cambiar el contrato.

## Tiempo estimado

60–120 minutos

## Competencias que desarrolla

- Lectura de contratos JSON Schema
- Explicación técnica en lenguaje simple
- Planificación de pipeline STT -> LLM -> TTS

## Conocimientos previos

- Sprint 1 C completado o al menos arrancado
- Ojeada a `contratos/inteligencia/v1/`
- Lectura de `documentacion/equipo/guia-rol-C-inteligencia.md`
- Lectura de `documentacion/equipo/clase-de-manana.md`

## Entregables

Lista concreta de archivos, capturas o evidencias:

1. `inteligencia/APRENDIZAJE.md` con sección **Contrato del turno** y ejemplo corto del flujo
2. `inteligencia/PRUEBA_STUB.md` o nota equivalente con pasos de health / infer-turn
3. Demostración verbal del stub vivo al equipo
4. Recordatorio escrito: C no escribe la DB de B; solo B llama a C

## Criterios de aceptación

Checklist binario (sí/no). Si uno falla, la misión no está hecha.

- [ ] Explicas entrada: `session_id`, `turn_id`, `student_id`, `audio.path`, `locale`
- [ ] Explicas salida: transcript, emotion, risk_signals, reply, tts, timing, metrics
- [ ] El stub arranca sin traceback
- [ ] Sabes el header `X-Internal-Token` y su valor de ejemplo en el proyecto
- [ ] No diste IA real por cerrada en este sprint

## Cómo validar el resultado

Pasos exactos que el estudiante (o el mentor) ejecuta para comprobar:

1. Arranca `python inteligencia/servidor_simulado.py`
2. Lee en voz alta el contrato del turno en menos de 1 minuto
3. Muestra `inteligencia/PRUEBA_STUB.md` y ejecuta el health o el paso documentado

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Copiar el schema sin explicarlo | Traducirlo a español con ejemplo |
| Llamar al stub desde Unity | Incorrecto: solo B llama a C |
| Querer IA real ya | Anótalo como siguiente fase; no bloquees el sprint |

## Qué NO debo hacer

- Cambiar schemas en `contratos/`
- Escribir en MySQL/SQLite de B
- Exponer Ollama, Whisper o cualquier servicio fuera de localhost

## Referencias técnicas (solo lectura / enlace)

- Guía de rol: `documentacion/equipo/guia-rol-C-inteligencia.md`
- Clase 2: `documentacion/equipo/clase-de-manana.md`
- Contratos: `contratos/inteligencia/v1/`
- Stub: `inteligencia/servidor_simulado.py`

## Evidencia de cierre

Cómo lo demuestras en la review (1–2 min):

- Stub vivo + 1 minuto explicando qué entra y qué sale del turno