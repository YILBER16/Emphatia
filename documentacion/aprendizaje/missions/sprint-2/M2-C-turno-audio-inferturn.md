# M2-C — Stub de turno listo para audio real + plan IA

## ID

`M2-C-turno-audio-inferturn`

## Título

Asegurar InferTurn ante audio del piloto y planificar STT→LLM→TTS

## Rol

C

## Objetivo

1) Que el stub (o el pipeline detrás del mismo contrato) responda bien cuando B envía un path de audio real del piloto.  
2) Dejar escrito el plan por etapas para sustituir stub por STT, LLM y TTS **sin cambiar** el contrato InferTurn.

## Tiempo estimado

90–150 minutos

## Competencias que desarrolla

- Contrato estable vs implementación  
- Prueba de integración B↔C  
- Planificación de deuda técnica controlada  

## Conocimientos previos

- Sprint 1 C (`PRUEBA_STUB`, APRENDIZAJE InferTurn)  
- Entender que solo B llama a C  

## Entregables

1. Stub arrancable; prueba documentada con B (o curl interno) usando un WAV en `datos/`  
2. Actualizar `inteligencia/PRUEBA_STUB.md` con caso “audio real del piloto”  
3. Sección en APRENDIZAJE: **Plan Sprint 3+** (STT → análisis → LLM → TTS) con checklist de aceptación futura  
4. Confirmar que `expression`/`timing` salen útiles para D/A (quality high|low)  
5. (Opcional spike) Anotar resultado de probar Whisper **sin** marcarlo como Done del sprint  

## Criterios de aceptación

- [ ] Health del stub OK  
- [ ] InferTurn documentado entra/sale (refresco)  
- [ ] Prueba conjunta con B al menos una vez  
- [ ] Plan IA real escrito (sin exigir instalación completa)  
- [ ] No escribes en DB de B  
- [ ] Checklist Sprint 2 C  

## Cómo validar

1. `python servidor_simulado.py`  
2. B hace un turno (o curl interno con token)  
3. Muestras JSON de respuesta y el plan STT/LLM/TTS  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Cambiar shape JSON “porque es más fácil” | Prohibido — Contract Review |
| Instalar 3 motores y no documentar | Primero contrato + stub estable |
| Exponer puerto a LAN | Solo localhost |

## Qué NO debo hacer

- Diagnosticar / medicar en replies  
- Unity directo  
- Marcar Ollama como cerrado sin integración con B  

## Bonus

- Spike Whisper en español con 1 WAV de prueba + métrica de tiempo en APRENDIZAJE  

## Referencias

- `contratos/inteligencia/v1/`  
- `inteligencia/servidor_simulado.py`  
- Guión clase Sprint 2  

## Evidencia de cierre

Demo B↔C + PRUEBA_STUB actualizado + plan IA.
