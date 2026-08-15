# Ejecutar EN EL PC DEL ROL C (PowerShell como Administrador si el firewall lo pide)
# Abre el stub de inteligencia a la red local para que B pueda llamarlo.

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..\inteligencia")

try {
    New-NetFirewallRule -DisplayName "EmpathIA Intel 8100" -Direction Inbound -LocalPort 8100 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue | Out-Null
} catch {
    Write-Host "No se pudo crear regla de firewall (abre PowerShell como Administrador)."
}

$env:INTEL_PORT = "8100"
Write-Host "Arrancando C en 0.0.0.0:8100 ..."
Write-Host "Prueba local: curl.exe -s http://127.0.0.1:8100/internal/v1/health"
python servidor_simulado.py
