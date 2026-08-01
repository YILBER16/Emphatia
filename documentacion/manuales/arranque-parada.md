# Arranque / parada — Fase 0

## Arrancar (orden)

### 1) Inteligencia (simulador C) — opcional si `INTEL_STUB=true`

```powershell
cd C:\laragon\www\Emphatia\inteligencia
python servidor_simulado.py
```

### 2) Servidor (B)

```powershell
cd C:\laragon\www\Emphatia\servidor
php artisan serve --host=127.0.0.1 --port=8000
```

### 3) Prueba de humo

```powershell
cd C:\laragon\www\Emphatia
powershell -ExecutionPolicy Bypass -File .\herramientas\prueba-humo-fase0.ps1
```

## Parar

Ctrl+C en cada terminal.

## Usuarios de prueba

| usuario | contraseña | rol |
|---------|------------|-----|
| estudiante1 | password | estudiante |
| orientador1 | password | orientador |
| admin1 | password | admin |
