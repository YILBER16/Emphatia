# M0-C — Entorno Inteligencia (Stub / InferTurn)

## ID

`M0-C-entorno-inteligencia`

## Título

Ejecutar el simulador y comprender InferTurn (lectura)

## Rol

C

## Objetivo

Que el stub de inteligencia arranque en tu PC y que puedas explicar, en español simple, qué entra y qué sale de un InferTurn — **sin** instalar modelos reales.

## Tiempo estimado

45–75 minutos

## Competencias que desarrolla

- Setup de servicio local  
- Lectura de contratos de API interna  
- Separación inferencia vs persistencia institucional  

## Conocimientos previos

- M0-COMUN  
- Python instalado (`python --version`)  

## Entregables

1. `python inteligencia/servidor_simulado.py` en ejecución  
2. Evidencia de que el proceso está vivo (log de consola o health)  
3. `inteligencia/APRENDIZAJE.md` con secciones: qué hace el simulador; qué es InferTurn; qué NO escribe C en la DB de B  
4. Lista de carpetas futuras `stt/`, `llm/`, `tts/`, `memory/` (pueden estar vacías)  

## Criterios de aceptación

- [ ] El simulador arranca sin traceback  
- [ ] Explicas InferTurn: entrada (audio/ids) → salida (texto, emoción, riesgo, reply, TTS, timing)  
- [ ] Sabes que solo B llama a C (`X-Internal-Token`)  
- [ ] Leíste (ojeaste) schemas en `contratos/inteligencia/v1/`  
- [ ] Checklist Sprint 0 C en verde  

## Cómo validar el resultado

1. Arranca el simulador; muestra la terminal.  
2. Abre APRENDIZAJE y explica InferTurn en ≤ 1 minuto.  
3. Señala los schemas de contrato (sin modificarlos).  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| `python` no reconocido | Reinstala Python marcando PATH |
| Puerto 8100 ocupado | Cierra el otro proceso |
| Quieres instalar Ollama “ya” | Fuera de Sprint 0 |
| Editas Laravel para “probar” | Pide a B; no toques `servidor/` |

## Qué NO debo hacer

- Whisper / Ollama / Kokoro en este sprint  
- Cambiar contratos de InferTurn  
- Escribir en MySQL/SQLite de B  

## Referencias técnicas

- `inteligencia/README.md`  
- `contratos/inteligencia/v1/*.schema.json`  
- `documentacion/equipo/guia-rol-C-inteligencia.md`  

## Evidencia de cierre

Simulador up + APRENDIZAJE con InferTurn explicado.
$env:VERTEX_AI_ENABLED = "true"
$env:VERTEX_AI_PROJECT = project-0c907c22-c264-4bec-813
$env:VERTEX_AI_LOCATION = "us-central1"
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\ruta-segura\service-account.json"
python servidor_simulado.py$env:VERTEX_AI_ENABLED = "true"
$env:GOOGLE_API_KEY = "AIzaSyA6UCT8dVn1sR3sukoZwpNL0tBgQIZmeXw"
python servidor_simulado.py