# Demo perfiles de estudiante (Fases 2–3) — Rol B

Uso (B arriba en :8000):

```powershell
cd C:\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\demo-perfiles-estudiante.ps1
# Opcional:
# .\herramientas\demo-perfiles-estudiante.ps1 -BaseUrl "http://192.168.1.31:8000/api/v1"
```

Qué prueba:
1. Login admin → crear perfil (muestra access_code una vez)
2. Regenerar código
3. Login orientador → listar estudiantes activos
4. Assume → crear sesión → enviar texto → poll events hasta turn.result

Requisitos: MySQL migrado + seed (`php artisan migrate --seed` en `backend/`).
