# Rol A — Cliente Unity (experiencia)

**Owner de:** `client-unity/`  
**Consume:** `contracts/rest/v1/`, `contracts/ws/v1/`, `contracts/expression/v1/`  
**Habla solo con:** Backend B (`127.0.0.1:8000`)

---

## Tu misión

Que el estudiante **entre, hable, vea/oiga al avatar y reciba respuesta**, sin que Unity “piense”. Tú eres la cara del sistema.

---

## Lecturas obligatorias (orden)

1. `docs/team/KICKOFF.md` + `ROLES.md` + `COLLABORATION.md`  
2. `contracts/rest/v1/openapi.yaml`  
3. `contracts/ws/v1/events.md` (y ADR-008: por ahora **poll de eventos**)  
4. `contracts/expression/v1/enums.md` + fixture en `expression/fixtures/`  
5. `docs/runbooks/ports.md`

---

## Qué ya puedes usar sin esperar a nadie

- Login / sesión / turnos / eventos / TTS vía API real de B (modo stub).  
- Smoke HTTP como referencia de secuencia: `tools/phase0-smoke.ps1`.  
- ExpressionPacket de ejemplo para probar boca/cara.

---

## Límites duros

- **Prohibido** llamar a `intelligence:8100`, Ollama o Whisper.  
- **Prohibido** inventar la respuesta del avatar si hay `turn.error`.  
- **Prohibido** cambiar el JSON de expresión sin PR a contratos + D.

---

## Entregables Fase 1 (tu DoD)

- [ ] Proyecto Unity 6 en `client-unity/` (o subcarpeta acordada).  
- [ ] Pantalla login (`estudiante1` / `password` en lab).  
- [ ] Crear / cerrar sesión de acompañamiento.  
- [ ] Captura de micrófono → archivo WAV → `POST .../turns` multipart.  
- [ ] Máquina de estados UI: `idle | listening | processing | speaking`.  
- [ ] Poll `GET .../events` hasta tener WS (F1.4 lo pone B; tú migrarás el cliente).  
- [ ] Reproducir TTS desde URL autenticada.  
- [ ] Aplicar `ExpressionPacket` (lips + face) con ayuda de D.  
- [ ] Manejo visible de `turn.error`.

---

## Orden de trabajo recomendado (A)

| Semana | Foco |
|--------|------|
| 1 | Proyecto Unity + login + sesión + estados UI (sin avatar final) |
| 1–2 | Mic + upload + poll eventos + texto en pantalla |
| 2–3 | Audio TTS + avatar básico |
| 3+ | Lip-sync / face con D (`timing_quality` high y low) |

---

## Cómo validar tú solo

1. B arriba con `INTEL_STUB=true`.  
2. Login → sesión → “enviar” WAV de prueba → ver `turn.result` en logs/UI.  
3. Reproducir TTS.  
4. Con fixture D, mover al menos 1 blendshape.

## Cómo validar con el equipo

Integration Friday: un turno completo con avatar en el PC piloto.

---

## Dependencias

| Necesitas de | Qué |
|--------------|-----|
| B | API estable; luego WS |
| D | Enums + mapeo blendshapes + criterio “aceptable” |
| C | Nada directo en Fase 1; calidad de audio/texto llega vía B |

---

## Branching

`a/feature-name` → PR → review preferente de A; si toca contrato, también B/D.
