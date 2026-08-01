# Rol D — Expression (labios, cara, sync)

**Owner de:** `expression/`, `contracts/expression/v1/`  
**Consumidores:** A (runtime Unity), C (timing), B (transporte del packet)

---

## Tu misión

Definir **cómo se ve y se sincroniza** el avatar con el audio/emoción, con un contrato estable.  
No entrenas el LLM; haces que la respuesta se sienta humana en la cara.

---

## Lecturas obligatorias

1. Kickoff + roles + colaboración  
2. `contracts/expression/v1/expression-packet.schema.json`  
3. `contracts/expression/v1/enums.md`  
4. `expression/fixtures/turn-result.expression.stub.json`  
5. `contracts/ws/v1/schemas/turn-result.payload.json` (campo `expression`)

---

## Qué ya está hecho (Fase 0)

- Schema ExpressionPacket v1.  
- Enums de visemas y gestos.  
- Fixture `timing_quality: low` usado por B/C stubs.

---

## Límites duros

- No implementes Whisper/Ollama “para generar visemas mágicos” sin contrato.  
- No pidas a A morph targets fuera del enum sin bump de versión.  
- Si el timing es pobre, **declararlo** (`low`), no fingir precisión.

---

## Entregables Fase 1–3 (tu DoD)

- [ ] Mantener schema + enums coherentes.  
- [ ] Guía de mapeo: cada `viseme` / `gesture` → blendshape(s) Unity (doc en `expression/`).  
- [ ] Fixtures: al menos 1 `low` y 1 `high` (aunque high sea sintético al inicio).  
- [ ] Criterio de aceptación lip-sync del piloto (medible: p.ej. desfase percibido / checklist demo).  
- [ ] Pairing con A: PR de integración en `client-unity` **revisado por ti**.  
- [ ] Tabla/reglas: `emotion_drive` → gesto facial por defecto.

---

## Orden de trabajo recomendado (D)

| Orden | Foco |
|-------|------|
| 1 | Congelar enums con A (¿el avatar tiene esos morphs?) |
| 2 | Doc de mapeo Unity |
| 3 | Fixture high sintético |
| 4 | Integración en A con audio stub |
| 5 | Cuando C dé timing real: calibrar y medir |

---

## Cómo validar tú solo

1. Validar JSON del fixture contra el schema (herramienta JSON Schema o revisión manual).  
2. En Unity (con A): reproducir WAV stub + aplicar lips del fixture.  
3. Anotar desfase y ajustar pesos/`t_ms`.

---

## Dependencias

| Necesitas de | Qué |
|--------------|-----|
| A | Avatar con blendshapes; tiempo de integración |
| C | `timing.cues` + `quality` |
| B | Que `turn.result.expression` llegue intacto |

**Importante:** tu código de runtime puede vivir en Unity bajo carpeta acordada con A (p.ej. `client-unity/.../Expression/`), pero el **contrato** vive en `contracts/expression` y los fixtures en `expression/`.

Branch: `d/...`
