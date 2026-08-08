# M2-D — Expresión dentro del turno

## ID

`M2-D-expresion-en-turno`

## Título

Cerrar morphs y preparar 1 expresión mínima en Speaking

## Rol

D

## Objetivo

1) Cerrar la tabla morphs con A (columna sí/no).  
2) Definir cómo A usa el `expression` de `turn.result` en estado Speaking (aunque sea 1 morph o boca abierta/cerrada).

## Tiempo estimado

60–120 minutos

## Competencias que desarrolla

- Cierre de interfaz A↔D  
- Criterio de aceptación visual de prototipo  
- Lectura de ExpressionPacket en contexto de turno  

## Conocimientos previos

- Sprint 1 D (fixture + enums)  
- Saber que el packet viaja en `turn.result`  

## Entregables

1. `expresion/TABLA_MORPHS.md` o sección final en APRENDIZAJE (≥ 5 visemas + 2 gestures, columna A completa)  
2. Mini-guía: “En Speaking, A debe…” (pasos 1-2-3)  
3. Pairing 15–20 min con A aplicando **al menos 1** morph o blendshape de prueba  
4. Nota de `timing_quality: low` (aceptable en piloto)  

## Criterios de aceptación

- [ ] Tabla cerrada con A (no todo “pendiente” sin hablar)  
- [ ] Guía Speaking publicada  
- [ ] Evidencia de 1 morph en Unity (captura o demo) **o** bloqueo técnico escrito si el avatar no tiene blendshapes  
- [ ] No se cambió schema sin review  
- [ ] Checklist Sprint 2 D  

## Cómo validar

1. Abrir tabla con A.  
2. En demo de turno (o simulado), A activa 1 morph.  
3. Mentor ve captura/demo ≤ 1 min.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Avatar sin morphs | Documentar subset o placeholder (escala de mandíbula) y plan |
| Pedir cine | Criterio piloto: “se nota que habla” |
| Trabajar sin A | La pareja es obligatoria |

## Qué NO debo hacer

- Implementar STT/LLM  
- Reemplazar el packet por otro formato  

## Bonus

- Mapa emotion_drive → gesture default  

## Referencias

- `contratos/expresion/v1/`  
- Fixture `paquete-expresion-ejemplo.json`  
- Misión A Sprint 2  

## Evidencia de cierre

Tabla + 1 morph en Speaking (o bloqueo documentado).
