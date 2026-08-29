# EmpathIA B — servidor + logs [A→B] en LA MISMA terminal (Windows).
# Uso:
#   cd C:\Emphatia\backend
#   powershell -ExecutionPolicy Bypass -File .\serve-lab.ps1

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$logDir = Join-Path $PSScriptRoot 'storage\logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$labLog = Join-Path $logDir 'lab-terminal.log'
'' | Set-Content -Path $labLog -Encoding UTF8

Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ' EmpathIA B LAB  http://0.0.0.0:8000' -ForegroundColor Cyan
Write-Host ' A debe usar: http://IP_DE_ESTE_PC:8000/api/v1' -ForegroundColor Yellow
Write-Host ' Logs en vivo abajo (Ctrl+C detiene todo)' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''

$php = Get-Command php -ErrorAction SilentlyContinue
if (-not $php) {
    Write-Host 'ERROR: php no está en el PATH.' -ForegroundColor Red
    exit 1
}

$serve = Start-Process -FilePath $php.Source `
    -ArgumentList @('artisan', 'serve', '--host=0.0.0.0', '--port=8000') `
    -WorkingDirectory $PSScriptRoot `
    -PassThru `
    -WindowStyle Minimized

Start-Sleep -Seconds 1
Add-Content -Path $labLog -Value ("[" + (Get-Date -Format 'HH:mm:ss') + "] [B] serve-lab iniciado pid=" + $serve.Id)
Write-Host ('[B] artisan serve pid=' + $serve.Id + ' (ventana minimizada)') -ForegroundColor Green
Write-Host 'Esperando [A→B] ...' -ForegroundColor Green
Write-Host ''

try {
    Get-Content -Path $labLog -Wait -Tail 80
}
finally {
    if ($serve -and -not $serve.HasExited) {
        Stop-Process -Id $serve.Id -Force -ErrorAction SilentlyContinue
        Write-Host ''
        Write-Host '[B] servidor detenido.' -ForegroundColor Yellow
    }
}
