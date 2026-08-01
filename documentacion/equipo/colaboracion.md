# Colaboración — Git, contratos y rituales

## Monorepo

```text
empathia/
  contracts/       ← todos (con review estricto)
  client-unity/    ← solo A mergea
  backend/         ← solo B mergea
  intelligence/    ← solo C mergea
  expression/      ← solo D mergea
  docs/            ← todos
  tools/           ← B coordina; PRs bienvenidos
```

## Branches

| Prefijo | Uso |
|---------|-----|
| `a/` | Unity |
| `b/` | Backend |
| `c/` | Intelligence |
| `d/` | Expression |
| `docs/` | Solo documentación |
| `contracts/` | Cambios de contrato (corto, con review) |

Ejemplos: `a/session-ui`, `b/mysql-phase1`, `c/whisper-es`, `d/viseme-map`.

`main` debe quedar siempre con smoke Fase 0 verde (o equivalente).

## Pull requests

1. Un PR = un objetivo claro.  
2. Descripción: qué / por qué / cómo probar.  
3. Si toca `contracts/`: sección **Breaking?** sí/no + versión.  
4. Owner de la carpeta aprueba.  
5. No usar `--no-verify` ni force-push a `main`.

## Contract Review (obligatorio si cambia contrato)

- Duración: 15–30 min.  
- Asisten productor + consumidores.  
- Salida: merge del contrato **antes** de implementar en módulos, o rechazo con alternativa.

## Rituales sugeridos

| Ritual | Cuándo | Agenda |
|--------|--------|--------|
| Standup | Diario, 10 min | Ayer / hoy / bloqueo (sobre todo puertos, PC, contratos) |
| Integration Friday | 1× semana | Un turno E2E en el PC piloto |
| Contract Review | Bajo demanda | Solo si hay PR a `contracts/` |

## Uso del PC piloto (1 máquina)

- Calendario compartido de franjas (Unity + modelos pesan).  
- Fuera de franja: trabajar contra **stubs** en portátil propio.  
- No dejar sesión `active` huérfana: cerrar sesión o avisar a B.

## Ambientes

| Variable / flag | Quién | Significado |
|-----------------|-------|-------------|
| `INTEL_STUB=true` | B | B no necesita C real |
| Stub Python C | C | InferTurn fake |
| Fixture Expression | D | Packet de prueba |
| Smoke script | Todos | Prueba de integración mínima |

## Definition of Done (equipo)

Una historia no está “terminada” si:

- rompe el smoke sin avisar, o  
- A/B/C/D no pueden integrarse con stub, o  
- el contrato quedó solo “en la cabeza” sin archivo en `contracts/`.
