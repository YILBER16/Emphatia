# M3-C — STT de audio y WAV que se oye

## ID

`M3-C-stt-y-wav`

## Título

Transcribir audio del piloto y devolver un TTS audible (mismo InferTurn)

## Rol

C

## Objetivo

1) Cuando B manda `audio.path`, C transcribe (Whisper o fallback **escrito**).  
2) C deja un archivo WAV con sonido (no silencio inaudible) en `tts.path`.  
3) El contrato InferTurn no cambia. Gemini y prompts siguen siendo el cerebro si están configurados.

## Tiempo estimado

120–180 minutos

## Competencias que desarrolla

- Pipeline STT → LLM → TTS detrás de un contrato estable  
- Degradación honesta (fallback documentado)  
- Integración con B en la misma máquina  

## Conocimientos previos

- Sprint 1–2 C: stub, PRUEBA_STUB, InferTurn  
- Whisper opcional ya esbozado en `servidor_simulado.py`  

## Entregables

1. InferTurn con audio: `transcript.text` sale del audio (o fallback + log claro)  
2. `tts.path` apunta a un WAV que **se oye** (beep, frase grabada, o Kokoro/otro TTS; silencio de 0 muestras no cuenta)  
3. Sección **Plan STT → LLM → TTS** en `inteligencia/APRENDIZAJE.md` (qué hay hoy vs Sprint 4)  
4. Actualizar `PRUEBA_STUB.md` con caso “WAV del piloto”  
5. Carpetas `inteligencia/stt/`, `llm/`, `tts/`, `memory/` (pueden estar casi vacías, con un README de 5 líneas)  
6. Checklist Sprint 3 C  

## Criterios de aceptación

- [ ] Health OK  
- [ ] Con `audio.path` válido, hay transcript (Whisper o fallback etiquetado en `model_versions.stt`)  
- [ ] `tts.path` existe y el archivo tiene duración > 0 audible  
- [ ] `timing.quality` es `low` o `high` (D/A pueden usarlo)  
- [ ] `preferred_name` sigue sanitizado; no escribes en la DB de B  
- [ ] Plan escrito; no exigimos Kokoro perfecto  
- [ ] Checklist Sprint 3 C  

## Cómo validar

1. `python inteligencia/servidor_simulado.py`  
2. B con `INTEL_STUB=false` hace un turno de audio **o** curl InferTurn con `audio.path` a un WAV en `datos/`  
3. Muestras JSON (transcript, reply, tts.path) y reproduces el WAV  
4. Mentor oye el archivo ≤ 1 min  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Cambiar el JSON “porque Whisper falla” | Fallback + `model_versions`; mismo schema |
| TTS silencio de Fase 0 | Generar beep o copiar un wav de prueba a `datos/audio/output/` |
| Exponer C a Internet | LAN del lab OK; no Ollama público |
| Pedir GPU como Done | CPU / modelo tiny es suficiente para el piloto |

## Qué NO debo hacer

- Auth institucional  
- Unity directo  
- Memoria persistente completa  
- Marcar Ollama como cerrado  

## Bonus

- Spike Kokoro ES o TTS local mínimo  
- `timing.cues` de 1 visema `aa` para D  

## Referencias

- `contratos/inteligencia/v1/`  
- `inteligencia/servidor_simulado.py`  
- `inteligencia/PRUEBA_STUB.md`  
- `documentacion/equipo/guia-rol-C-inteligencia.md`  

## Evidencia de cierre

JSON InferTurn de un WAV real + archivo de salida que se oye + plan escrito.
