# M1-C — InferTurn con stub demostrable

## ID

`M1-C-inferturn-stub`

## Título

Explicar y demostrar el contrato InferTurn (simulador)

## Rol

C

## Objetivo

Que el equipo entienda qué hace Inteligencia en un turno y que puedas **demostrar el stub** vivo, dejando escrito el contrato en español simple — base para la IA real en sprints siguientes.

## Tiempo estimado

60–100 minutos

## Competencias que desarrolla

- Lectura de contratos JSON Schema  
- Comunicación técnica simple  
- Operación de servicio stub  

## Conocimientos previos

- Sprint 0 C (simulador arranca)  
- Ojeada a `contratos/inteligencia/v1/`  

## Entregables

1. `inteligencia/APRENDIZAJE.md` con sección **Contrato del turno (InferTurn)**  
2. `inteligencia/PRUEBA_STUB.md` con pasos de prueba (health y/o POST documentado)  
3. Simulador corriendo en la demo de cierre  
4. Recordatorio escrito: C no escribe la DB de B; solo B llama a C  

## Criterios de aceptación

- [ ] Explicas entrada: `session_id`, `turn_id`, `student_id`, `audio.path`, `locale`  
- [ ] Explicas salida: transcript, emotion, risk_signals, reply, tts, timing, metrics  
- [ ] Stub arranca sin traceback  
- [ ] Sabes el header `X-Internal-Token` (valor de ejemplo del proyecto)  
- [ ] No instalaste Ollama/Whisper como “done” de este sprint  
- [ ] Checklist Sprint 1 C en verde  

## Cómo validar el resultado

1. Arranca `python inteligencia/servidor_simulado.py`.  
2. Lee en voz alta el contrato (≤ 1 min).  
3. Muestra PRUEBA_STUB.md y ejecuta al menos el health o el paso que documentaste.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Copiar el schema sin explicarlo | Traducir a español con ejemplo |
| Llamar al stub desde Unity | Incorrecto — solo B |
| Querer IA real ya | Anótalo como “próximo sprint”; no bloquees M1 |

## Qué NO debo hacer

- Cambiar schemas en `contratos/`  
- Escribir en MySQL/SQLite de B  
- Exponer Ollama a la red  

## Bonus

- Diagrama ASCII del pipeline STT→LLM→TTS dentro de APRENDIZAJE  
- Lista de modelos que probarás en Sprint 2 (solo nombres, sin instalar)  

## Referencias

- `contratos/inteligencia/v1/*.schema.json`  
- `inteligencia/servidor_simulado.py`  
- `documentacion/equipo/guia-rol-C-inteligencia.md`  
- `documentacion/equipo/clase-2.md`  

## Evidencia de cierre

Stub vivo + 1 minuto de explicación InferTurn.
