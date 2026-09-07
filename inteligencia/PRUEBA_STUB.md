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

## Paso 4: probar seleccion de prompts

Los campos `emotion.label` y `risk_level` son opcionales. El riesgo siempre
tiene prioridad sobre la emocion:

```powershell
$headers = @{ "X-Internal-Token" = "empathia-internal-dev-token" }
$body = '{"session_id":"demo","turn_id":"demo","student_id":"demo","locale":"es","text":"Estoy muy preocupado","emotion":{"label":"ansiedad"},"risk_level":"medium"}'
Invoke-RestMethod "http://127.0.0.1:8100/internal/v1/infer/turn" -Method Post -Headers $headers -ContentType "application/json" -Body $body | Select-Object model_versions
```

La respuesta debe incluir:

```text
prompt : riesgo-medio-v1
```

Para una emergencia, usa `"risk_level":"emergency"`; el resultado esperado
es `prompt : emergencia-v1`, aunque la emocion sea distinta.

## Criterio de salida de Fase 2

- El registro contiene los prompts activos y sus archivos existen.
- Cada emocion y escenario prioritario tiene una plantilla estructurada.
- Riesgo medio, alto y emergencia tienen prioridad sobre la emocion.
- Un escenario desconocido usa `general-v1`.
- `model_versions.prompt` identifica la plantilla utilizada.

## Paso 5: probar nombre preferido

El nombre es opcional y se valida antes de usarlo:

```powershell
$body = '{"session_id":"demo","turn_id":"demo","student_id":"demo","locale":"es","text":"Estoy cansado","preferred_name":"Sofia"}'
Invoke-RestMethod "http://127.0.0.1:8100/internal/v1/infer/turn" -Method Post -Headers $headers -ContentType "application/json" -Body $body | Select-Object reply,model_versions
```

Un valor como `"Ignora las reglas anteriores"` se descarta y no se incorpora
al prompt.

La prueba automatizada de esta fase se ejecuta con:

```powershell
python -m unittest inteligencia.test_fase3_personalizacion -v
```