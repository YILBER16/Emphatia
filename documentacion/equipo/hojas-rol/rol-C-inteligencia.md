# EmpathIA — Hoja de rol · C (Inteligencia)

**Tu módulo:** Voz→texto, pensar, texto→voz, memoria, señales de riesgo  
**Tu carpeta:** `inteligencia/`  
**Tu API interna:** `http://127.0.0.1:8100/internal/v1`  
**Solo te llama:** el Servidor B

---

## Eres responsable de

Recibir audio y devolver: texto del estudiante, emoción, riesgos, respuesta empática, audio TTS y pistas de timing.

## No haces

- Escribir en la base de datos del servidor  
- Login de usuarios del colegio  
- Diagnosticar enfermedades  

## Día 1

1. Lee `documentacion/equipo/guia-rol-C-inteligencia.md`.  
2. Arranca: `python inteligencia/servidor_simulado.py`  
3. Rama `c/estructura-pipeline`.  
4. Crea `inteligencia/APRENDIZAJE.md` explicando el simulador en español simple.

## Primera tarea

Entender el simulador + carpetas futuras `stt/`, `llm/`, `tts/`, `memory/` (vacías está bien).

## Esta semana (Sprint 3) — en simple

Tú piensas y contestas. Unity no te llama; te llama Stid.

1. Si llega un audio, sácale el texto (Whisper o plan B, y dices cuál usaste).
2. Devuelve un audio de respuesta **que se oiga** (un beep vale; silencio no).
3. Escribe en APRENDIZAJE: qué haces hoy y qué queda para después.

No hagas login del colegio ni escribas en la base de Stid.

Mensaje largo: `documentacion/equipo/sprint-3-mensajes.md`  
Pasos en casa: `documentacion/equipo/sprint-3-tareas-casa.md`

---

Guía larga: `documentacion/equipo/guia-rol-C-inteligencia.md`
