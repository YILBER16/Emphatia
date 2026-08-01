# Expression enums v1 (module D)

## Visemes (`lips[].viseme`)

Aligned to a compact ARKit-like subset for MVP lip-sync:

| Code | Intended mouth shape |
|------|----------------------|
| sil | Silence / closed |
| PP | P, B, M |
| FF | F, V |
| TH | Th |
| DD | D, T, N |
| kk | K, G |
| CH | Ch, J, Sh |
| SS | S, Z |
| nn | N |
| RR | R |
| aa | A |
| E | E |
| I | I |
| O | O |
| U | U |

## Face gestures (`face[].gesture`)

| Code | Use |
|------|-----|
| neutral | Default |
| soft_smile | Warm positive |
| soft_concern | Empathic concern |
| warm_attention | Listening / present |
| calm | Soothing |

## Mapping ownership

- D owns this document and ExpressionPacket schema.
- C provides `timing.cues` + emotion.
- B (or C) assembles ExpressionPacket per fixtures in `/expression/fixtures`.
- A maps codes → Unity blendshapes / morph targets.
