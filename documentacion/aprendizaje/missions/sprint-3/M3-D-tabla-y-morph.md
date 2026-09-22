# M3-D — Tabla de morphs y 1 expresión en el turno

## ID

`M3-D-tabla-y-morph`

## Título

Poner al día expresión: contrato, tabla con A, un morph en Speaking

## Rol

D

## Objetivo

Este rol no cerró Sprints 0–2. En Sprint 3 se **recupera** lo esencial y se entrega el mínimo del mapa: A sabe qué morph mover cuando el avatar habla.

## Tiempo estimado

120–180 minutos (incluye catch-up)

## Competencias que desarrolla

- Lectura de ExpressionPacket  
- Interfaz A↔D  
- Criterio de prototipo (“se nota que habla”), no cine  

## Conocimientos previos

- Leer `contratos/expresion/v1/` y el fixture  
- 20 min de pairing con A frente al avatar  

## Entregables

1. `expresion/APRENDIZAJE.md` (qué es visema, gesture, qué NO hace D)  
2. `expresion/TABLA_MORPHS.md`: ≥ 5 visemas + 2 gestures, columna **¿A lo tiene?** (sí / no / pendiente)  
3. Mini-guía: “En Speaking, A debe…” (3 pasos)  
4. Pairing: **1 morph** demostrado en Unity **o** bloqueo técnico (avatar sin blendshapes + plan)  
5. Nota: `timing_quality: low` es aceptable en el piloto  
6. Checklist Sprint 3 D  

## Criterios de aceptación

- [ ] APRENDIZAJE existe y se explica en ≤ 1 min  
- [ ] Tabla basada en `enums.md`, no inventada  
- [ ] Pareja con A hecha (≥ 15 min)  
- [ ] 1 morph en demo **o** bloqueo escrito  
- [ ] No se cambió el schema sin Contract Review  
- [ ] Checklist Sprint 3 D  

## Cómo validar

1. Abrir fixture + tabla con A.  
2. En un turno (o Play Mode), A activa 1 morph.  
3. Mentor ve captura o demo ≤ 1 min.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Pedir 15 visemas el primer día | Un morph basta para Sprint 3 |
| Programar Unity sin A | D especifica; A implementa (salvo pairing) |
| Cambiar InferTurn | Fuera de rol |

## Qué NO debo hacer

- Pipeline STT/LLM  
- Historial de riesgo  
- Lip-sync fotograma a fotograma  

## Bonus

- Mapa `emotion_drive` → 1 gesture (`soft_concern` / `soft_smile`)  
- Fixture extra `timing_quality: low` copiado para el piloto  

## Referencias

- `contratos/expresion/v1/enums.md`  
- `expresion/fixtures/paquete-expresion-ejemplo.json`  
- Misiones M0-D, M1-D, M2-D (este archivo las sustituye como catch-up)  
- `documentacion/equipo/guia-rol-D-expresion.md`  

## Evidencia de cierre

Tabla + 1 morph (o bloqueo) + APRENDIZAJE.

**Si no hay estudiante D:** A + mentor completan tabla y 1 morph; dejan nota en APRENDIZAJE de A.
