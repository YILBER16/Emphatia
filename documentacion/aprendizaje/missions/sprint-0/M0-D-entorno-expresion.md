# M0-D — Entorno Expresión (ExpressionPacket + fixtures)

## ID

`M0-D-entorno-expresion`

## Título

Comprender ExpressionPacket y validar el fixture de ejemplo

## Rol

D

## Objetivo

Entender el contrato de expresión, saber qué es un visema/gesture, y validar que el fixture de ejemplo es coherente con el schema — **sin** programar Unity.

## Tiempo estimado

45–75 minutos

## Competencias que desarrolla

- Lectura de schemas JSON  
- Pensamiento de interfaz entre sistemas  
- Comunicación de requisitos a A  

## Conocimientos previos

- M0-COMUN  
- Saber qué es un archivo JSON  

## Entregables

1. `expresion/APRENDIZAJE.md` explicando ExpressionPacket, visemas y gestures en español simple  
2. Notas de revisión del fixture `expresion/fixtures/paquete-expresion-ejemplo.json`  
3. Lista inicial de morphs que pedirás a A (borrador)  
4. Confirmación de haber leído `contratos/expresion/v1/` (schema + enums)  

## Criterios de aceptación

- [ ] Explicas qué campos tiene un ExpressionPacket (versión, lips, face, timing_quality, emotion_drive)  
- [ ] Abres el fixture y señalas al menos 3 visemas y 1 gesture  
- [ ] Sabes que D no implementa TTS  
- [ ] Sabes que cambios al schema requieren Contract Review  
- [ ] Checklist Sprint 0 D en verde  

## Cómo validar el resultado

1. Abre schema + enums + fixture en Cursor (3 pestañas).  
2. Lee APRENDIZAJE 30–60 s al mentor.  
3. Di un ejemplo: “si llega visema `aa` en t=120, A debería…”.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Empezar scripts en Unity | Eso es con A en sprint posterior |
| Cambiar enums “porque se ve mejor” | Propón PR a contratos; no mergees solo |
| Confundir emotion_drive con gesture | Relee ROLE_OVERVIEW y enums |

## Qué NO debo hacer

- Modificar InferTurn  
- Guardar riesgo en DB  
- Romper el fixture sin avisar a A/C/B  

## Referencias técnicas

- `contratos/expresion/v1/expression-packet.schema.json`  
- `contratos/expresion/v1/enums.md`  
- `expresion/fixtures/paquete-expresion-ejemplo.json`  
- `documentacion/equipo/guia-rol-D-expresion.md`  

## Evidencia de cierre

APRENDIZAJE + recorrido guiado del fixture.
