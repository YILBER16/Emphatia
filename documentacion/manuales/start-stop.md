# Start / Stop — Fase 0

## Start (orden)

En tres terminales (desde la raíz del monorepo):

### 1) Intelligence stub (C)

```powershell
cd intelligence
python stub_server.py
```

Health: `http://127.0.0.1:8100/internal/v1/health`

### 2) Backend Laravel (B)

```powershell
cd backend
php artisan serve --host=127.0.0.1 --port=8000
```

Health: `http://127.0.0.1:8000/api/v1/health`

### 3) Smoke vertical slice

```powershell
.\tools\phase0-smoke.ps1
```

## Stop

Ctrl+C en cada proceso. No deja sesión huérfana crítica en Fase 0 (SQLite local).

## Usuarios piloto (seed)

| username | password | role |
|----------|----------|------|
| estudiante1 | password | student |
| orientador1 | password | counselor |
| admin1 | password | admin |
