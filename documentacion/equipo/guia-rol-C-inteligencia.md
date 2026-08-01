# Rol C — Intelligence (pipeline de IA)

**Owner de:** `intelligence/`  
**Implementa:** `contracts/intelligence/v1/`  
**Idioma:** español (`locale: es`)

---

## Tu misión

Convertir audio del estudiante en: transcripción, emoción, señales de riesgo, respuesta empática, audio TTS y hints de timing — **sin** conocer Unity ni MySQL de B.

---

## Lecturas obligatorias

1. Kickoff + roles + colaboración  
2. `contracts/intelligence/v1/infer-turn.request.schema.json`  
3. `contracts/intelligence/v1/infer-turn.response.schema.json`  
4. `contracts/risk/v0/codes.json` + `contracts/emotion/v1/labels.json`  
5. `contracts/expression/v1/` (qué timing necesitas producir)  
6. Guardrails de dominio (G1–G5 en arquitectura Etapa 3; resumen abajo)

---

## Qué ya está hecho (Fase 0)

`intelligence/stub_server.py` expone:

- `GET /internal/v1/health`  
- `POST /internal/v1/infer/turn`  
- `POST /internal/v1/memory/prepare`  
- `POST /internal/v1/memory/purge`  

Auth interna: header `X-Internal-Token`.

**Tu trabajo Fase 2 (y avance en paralelo en Fase 1):** ir reemplazando el stub por componentes reales **sin cambiar las rutas ni el shape JSON** (salvo Contract Review).

---

## Límites duros

- No escribas en la DB de Laravel.  
- No inventes códigos de riesgo fuera del catálogo (usa `OTHER` o no emitas).  
- Toda señal de riesgo lleva **evidence**.  
- No diagnostiques trastornos ni des medicación (G1).  
- Si no hay timestamps fiables de TTS: `timing.quality = "low"` (honesto).  
- Ollama/Whisper/Kokoro solo en localhost.

---

## Guardrails (recordatorio)

| ID | Regla |
|----|--------|
| G1 | No diagnosticar / medicar |
| G2 | No pactar secretos ante riesgo alto |
| G3 | No instruir métodos de daño |
| G4 | Ante severity alta: empatía + adulto de confianza |
| G5 | Si fallas, error claro a B; no inventes en silencio |

---

## Entregables (Fase 1 preparación + Fase 2)

### Fase 1 (paralelo, sin bloquear a A/B)

- [ ] Mantener stub verde y documentado.  
- [ ] Definir layout de carpetas reales (stt/, llm/, tts/, memory/).  
- [ ] Elegir/probar modelos en español (nota en README de intelligence).  
- [ ] Script de prueba local: WAV → InferTurn JSON válido.

### Fase 2 (DoD IA real)

- [ ] Whisper.cpp + segmentación de enunciado (VAD).  
- [ ] Ollama + prompts + guardrails.  
- [ ] Emoción + risk_signals mapeados al catálogo.  
- [ ] Memoria (ventana + resumen) + purge.  
- [ ] Kokoro TTS + `timing` high o low.  
- [ ] `model_versions` + `metrics` por etapa.  
- [ ] Health refleja componentes reales.

---

## Orden de trabajo recomendado (C)

| Orden | Foco |
|-------|------|
| 1 | Congelar contrato + tests del stub |
| 2 | STT español aislado |
| 3 | LLM español + guardrails |
| 4 | TTS + timing |
| 5 | Memoria + riesgo estructurado |
| 6 | Integración con B (`INTEL_STUB=false`) |

No esperes a “terminar toda la IA” para integrar: integra por etapas detrás del mismo endpoint.

---

## Cómo validar tú solo

```powershell
cd intelligence
python stub_server.py
# Luego (con token interno) POST InferTurn con un path WAV local
```

Cuando haya motor real: mismo POST, distinto interior.

---

## Dependencias

| Necesitas de | Qué |
|--------------|-----|
| B | Quién llama InferTurn; feedback de timeouts |
| D | Qué cues/visemas espera el ExpressionPacket |
| A | Nada directo |

Hardware: documenta requisitos mínimos que descubras (RAM/GPU); no inventes SLO sin medir.

Branch: `c/...`
