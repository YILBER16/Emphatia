# Puertos — PC Windows (piloto)

| Servicio | Puerto | Escucha | Rol |
|----------|--------|---------|-----|
| Servidor B | 8000 | 127.0.0.1 | B |
| Inteligencia C | 8100 | 127.0.0.1 | C |
| MySQL (después) | 3306 | 127.0.0.1 | B |
| Ollama (después) | 11434 | 127.0.0.1 | C |

## Carpeta de datos

```text
{repo}/datos/
  audio/input/
  audio/output/
  inteligencia/memory/
  inteligencia/tmp/
  logs/b/
  logs/c/
```

Variable: `EMPATHIA_DATA_ROOT` (ruta absoluta recomendada).

No abras estos puertos a la red del colegio en el prototipo.
