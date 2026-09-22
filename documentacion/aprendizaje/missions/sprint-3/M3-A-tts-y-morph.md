# M3-A — TTS audible y un morph en Speaking

## ID

`M3-A-tts-y-morph`

## Título

Oír la respuesta y mover 1 morph mientras el avatar “habla”

## Rol

A

## Objetivo

Que Unity deje de ser solo texto en pantalla: reproducir el WAV de TTS que entrega B y aplicar **un** morph/blendshape en estado `speaking`, según el `expression` de `turn.result` o la guía de D.

## Tiempo estimado

120–180 minutos

## Competencias que desarrolla

- Playback autenticado  
- Máquina de estados listening / processing / speaking  
- Consumo de ExpressionPacket (mínimo)  

## Conocimientos previos

- Sprint 2 A: sesión, turno, poll, `reply_text`  
- D (o mentor) te dice qué morph existe en el avatar  

## Entregables

1. Tras `turn.result`, se **oye** TTS (no solo “intenté descargar”)  
2. Estado `speaking` visible mientras suena; vuelve a `idle` al terminar  
3. **1 morph** activo en Speaking (boca abierta, jaw, visema `aa`, o el que acuerdes con D)  
4. El camino principal del turno de audio sigue siendo **POST a B** (no a `:8100`)  
5. Nota en `cliente-unity/APRENDIZAJE.md`: STT local de Windows = fallback de lab, no el diseño  
6. Checklist Sprint 3 A  

## Criterios de aceptación

- [ ] Reproduzco TTS con Bearer y se oye en el piloto  
- [ ] Si no hay audio, muestro error en español (no silencio eterno sin mensaje)  
- [ ] Aplico 1 morph en Speaking **o** dejo bloqueo escrito con D (avatar sin blendshapes)  
- [ ] No llamo a Inteligencia (`:8100`)  
- [ ] No presento el DictationRecognizer de Windows como el STT oficial del prototipo  
- [ ] Checklist Sprint 3 A  

## Cómo validar

1. B y C arriba en el piloto (`INTEL_STUB=false` si B ya lo tiene).  
2. Login adulto → estudiante → sesión → texto **y** un WAV.  
3. Se lee `reply_text`, se oye TTS, se ve 1 morph.  
4. Demo ≤ 2 min al mentor.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| TTS 401 | Header Authorization en la descarga |
| WAV silencio | Coordinar con B/C: el archivo debe tener muestras audibles |
| Morph “inventado” | Usar la tabla de D; un solo blendshape basta |
| STT solo local | Enviar audio a B; C transcribe en este sprint |

## Qué NO debo hacer

- Lip-sync de todos los visemas  
- Hablar con C directo  
- Cambiar contratos  
- WebSockets  

## Bonus

- Mostrar `transcript` de C (no solo el texto que tipeó el lab)  
- Apagar el morph al terminar el clip  

## Referencias

- `contratos/expresion/v1/`  
- Misión D Sprint 3  
- `cliente-unity/APRENDIZAJE.md`  
- `documentacion/equipo/guia-rol-A-avatar.md`  

## Evidencia de cierre

Pantalla + audio + 1 morph (o bloqueo D) en un turno real contra B.
