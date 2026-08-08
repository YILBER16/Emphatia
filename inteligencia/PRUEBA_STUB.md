# PRUEBA_STUB — Rol C

## Objetivo

Tener una prueba simple y repetible para demostrar que el simulador de Inteligencia sigue vivo.

## Paso 1: arrancar el stub

```powershell
python inteligencia/servidor_simulado.py
```

## Paso 2: health

Con el stub arriba, comprobar:

```powershell
curl http://127.0.0.1:8100/internal/v1/health
```

Debe responder `status: ok`.

## Paso 3: infer/turn

Ejemplo de validación mínima:

```powershell
curl -X POST http://127.0.0.1:8100/internal/v1/infer/turn `
  -H "Content-Type: application/json" `
  -H "X-Internal-Token: empathia-internal-dev-token" `
  -d '{"session_id":"demo","turn_id":"demo","student_id":"demo","locale":"es"}'
```

## Criterio de salida

- No hay traceback.
- La respuesta contiene `transcript`, `emotion`, `reply`, `tts`, `timing` y `metrics`.
- Si se usa audio real, `audio.path` debe existir y el contrato no cambia.