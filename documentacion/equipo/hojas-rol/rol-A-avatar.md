# EmpathIA — Hoja de rol · A (Avatar / Unity)

**Tu módulo:** Cliente / avatar / voz en Unity  
**Tu carpeta:** `cliente-unity/`  
**Solo hablas con:** Servidor B → `http://127.0.0.1:8000`

---

## Eres responsable de

Escuchar al estudiante, enviar el audio, mostrar estados, reproducir la respuesta y animar labios/cara según el paquete de expresión.

## No haces

- Llamar a Inteligencia (`:8100`), Ollama o Whisper  
- Inventar respuestas si hay error  
- Cambiar contratos sin avisar al equipo  

## Día 1 (haz esto hoy)

1. Lee `documentacion/equipo/guia-rol-A-avatar.md` (guía completa).  
2. Corre la prueba de humo: `.\herramientas\prueba-humo-fase0.ps1` (con el servidor arriba).  
3. Crea rama `a/proyecto-unity`.  
4. Crea el proyecto Unity 6 en `cliente-unity/`.

## Primera tarea

Login → sesión → grabar WAV → enviar turno → ver eventos → mostrar el texto de respuesta.

## Te bloqueas si…

Morphs del avatar → habla con **D**.  
La API falla → habla con **B**.  
Nunca con C directo.

## Éxito mínimo (prototipo)

Estados escuchando / pensando / hablando + audio + boca básica + error visible.

---

Guía larga: `documentacion/equipo/guia-rol-A-avatar.md`
