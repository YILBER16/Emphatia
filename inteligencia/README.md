# Inteligencia (módulo C)

Fase 0: simulador

```powershell
python servidor_simulado.py
```

Más adelante: Whisper, Ollama, Kokoro detrás del mismo contrato en `contratos/inteligencia/`.

## Vertex AI (Gemini)

El endpoint que usa B se mantiene: `POST /internal/v1/infer/turn`. Cuando
`VERTEX_AI_ENABLED=true`, las solicitudes con `text` generan `reply.text` con
Gemini. Unity no llama a Google directamente.

Instala las dependencias y configura variables en la terminal que inicia C:

```powershell
python -m pip install -r requirements.txt
$env:VERTEX_AI_ENABLED = "true"
$env:VERTEX_AI_PROJECT = "TU_PROYECTO"
$env:VERTEX_AI_LOCATION = "us-central1"
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\ruta-segura\service-account.json"
python servidor_simulado.py
```

La cuenta de servicio debe contar con el rol `Vertex AI User` y la API Vertex
AI debe estar habilitada en el proyecto. Para verificar la configuracion desde
B o C, usa el endpoint protegido:

```powershell
$headers = @{ "X-Internal-Token" = "empathia-internal-dev-token" }
Invoke-RestMethod "http://127.0.0.1:8100/internal/v1/vertex/health" -Headers $headers
```

### Autenticacion con API key

Si Google Console proporciono una API key, no se usa el JSON de cuenta de
servicio. Define la clave solo en la terminal y arranca el servicio desde esa
misma terminal:

```powershell
$env:VERTEX_AI_ENABLED = "true"
$env:GOOGLE_API_KEY = "PEGA_LA_CLAVE_SOLO_EN_TU_TERMINAL"
python servidor_simulado.py
```

No guardes la clave en el repositorio ni la compartas por chat. El endpoint de
health mostrara `authentication: "api_key"`, pero nunca devolvera la clave.
