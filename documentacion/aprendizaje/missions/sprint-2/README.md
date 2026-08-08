# Sprint 2 — Turno con audio y base real

## Objetivo del sprint

Pasar del stub demostrable a un **turno con audio mejor preparado**: el equipo entiende qué entra, qué sale y cómo se va a sustituir el stub por componentes reales en etapas.

## Resultado esperado (equipo)

| Rol | Hecho |
|-----|-------|
| A | Tiene claro qué pedirá al turno de C y cómo lo consumirá |
| B | Consume el contrato de C sin inventar shape nuevo |
| C | Explica el contrato del turno y demuestra el stub vivo |
| D | Deja claro qué timing/visemas necesita A |

## Misiones

1. [M2-C](./M2-C-turno-audio-inferturn.md)
2. En paralelo, el resto de roles documenta el puente de sesión 2 según la clase de mañana.

## Definition of Done — Sprint 2

- [ ] C puede explicar en español simple qué recibe y qué devuelve InferTurn
- [ ] El stub sigue respondiendo sin traceback
- [ ] El equipo tiene una máquina piloto y un flujo de demo acordado
- [ ] Nadie marcó IA real como cerrada sin haberla integrado por etapas

## Fuera de alcance

WebSockets reales, IA en red, cambios de contrato en `contratos/`, escribir en la DB de B, exponer servicios fuera de localhost.