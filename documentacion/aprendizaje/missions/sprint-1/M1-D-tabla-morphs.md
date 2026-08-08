# M1-D — Tabla de morphs para el Avatar

## ID

`M1-D-tabla-morphs`

## Título

Acordar con A cómo se mapeará ExpressionPacket a Unity

## Rol

D

## Objetivo

Dejar una **tabla usable** visema/gesture → morph Unity, validada en pareja con A, para que el lip-sync futuro no se improvise.

## Tiempo estimado

60–90 minutos

## Competencias que desarrolla

- Traducción contrato → implementación  
- Colaboración A↔D  
- Criterios de aceptación visuales  

## Conocimientos previos

- Sprint 0 D (fixture + enums leídos)  
- Acceso a hablar con A  

## Entregables

1. Tabla en `expresion/APRENDIZAJE.md` (o `expresion/TABLA_MORPHS.md`)  
2. Columna **¿A lo tiene?** llena en sesión con A (sí / no / pendiente)  
3. Sección “Cómo lo usará A en el turno” (paso a paso corto)  
4. Mínimo 5 visemas + 2 gestures documentados  

## Criterios de aceptación

- [ ] Tabla basada en `contratos/expresion/v1/enums.md`  
- [ ] Referencia al fixture `paquete-expresion-ejemplo.json`  
- [ ] Pareja A↔D hecha (al menos 10 min)  
- [ ] Al menos 3 ítems marcados sí o pendiente con dueño  
- [ ] No se cambió el schema sin Contract Review  
- [ ] Checklist Sprint 1 D en verde  

## Cómo validar el resultado

1. Abrir la tabla con A presente.  
2. A señala un morph que sí tiene.  
3. D explica un visema del fixture → morph.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Tabla inventada sin enums | Volver al contrato |
| “Todos pendiente” sin hablar con A | Hacer la pareja obligatoria |
| Pedir 50 morphs | Empezar por el subset del enum MVP |

## Qué NO debo hacer

- Programar el runtime Unity por A  
- Cambiar InferTurn  
- Prometer lip-sync perfecto esta clase  

## Bonus

- Criterio escrito: “demo aceptable = boca se mueve en Speaking aunque sea rough”  
- Fixture `high` sintético adicional (opcional)  

## Referencias

- `contratos/expresion/v1/`  
- `expresion/fixtures/paquete-expresion-ejemplo.json`  
- `documentacion/equipo/guia-rol-D-expresion.md`  
- `documentacion/equipo/clase-2.md`  

## Evidencia de cierre

Tabla proyectada + A confirma 1 morph en vivo.
