# Puertos — EmpathIA nodo Windows (piloto)

| Servicio | Puerto | Bind | Owner |
|----------|--------|------|-------|
| Laravel B (HTTP + events poll Phase 0) | 8000 | 127.0.0.1 | B |
| Intelligence C | 8100 | 127.0.0.1 | C |
| MySQL (Fase 1+) | 3306 | 127.0.0.1 | B |
| Ollama (Fase 2+) | 11434 | 127.0.0.1 | C |

## data_root

Ruta por defecto (relativa al monorepo):

```text
{repo}/data_root/
  audio/input/
  audio/output/
  intelligence/memory/
  intelligence/tmp/
  logs/b/
  logs/c/
```

Configurar con `EMPATHIA_DATA_ROOT` (absoluta recomendada en piloto).

**No exponer** 8000/8100/11434 a la LAN en el MVP.
