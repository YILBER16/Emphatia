# EmpathIA Phase 0 vertical-slice smoke test
# Requires: backend on :8000 (INTEL_STUB=true)

$ErrorActionPreference = "Stop"
$base = "http://127.0.0.1:8000/api/v1"
$repo = Split-Path -Parent $PSScriptRoot
$tmpWav = Join-Path $env:TEMP "empathia-phase0-input.wav"

function Write-SilentWav([string]$path) {
    $sampleRate = 16000
    $numSamples = [int]($sampleRate / 4)
    $data = New-Object byte[] ($numSamples * 2)
    $stream = [System.IO.File]::Create($path)
    $bw = New-Object System.IO.BinaryWriter($stream)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
    $bw.Write([int](36 + $data.Length))
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
    $bw.Write([int]16)
    $bw.Write([int16]1)
    $bw.Write([int16]1)
    $bw.Write([int]$sampleRate)
    $bw.Write([int]($sampleRate * 2))
    $bw.Write([int16]2)
    $bw.Write([int16]16)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
    $bw.Write([int]$data.Length)
    $bw.Write($data)
    $bw.Close()
}

Write-Host "== Health =="
$health = Invoke-RestMethod -Uri "$base/health" -Method GET
Write-Host ($health | ConvertTo-Json -Compress)
if ($health.status -ne "ok" -and $health.status -ne "degraded") {
    throw "Backend health not ok"
}

Write-Host "== Login =="
$loginBody = '{"username":"estudiante1","password":"password"}'
$login = Invoke-RestMethod -Uri "$base/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $login.token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "user=$($login.user.username) role=$($login.user.role)"

Write-Host "== Create session =="
$sessionResp = Invoke-RestMethod -Uri "$base/accompaniment/sessions" -Method POST -Headers $headers -Body '{"locale":"es","client":"unity"}' -ContentType "application/json"
$sessionId = $sessionResp.session.id
Write-Host "session_id=$sessionId"

Write-Host "== Upload turn (curl multipart) =="
Write-SilentWav $tmpWav
$clientTurnKey = [guid]::NewGuid().ToString()
$turnJson = curl.exe -s -X POST "$base/accompaniment/sessions/$sessionId/turns" `
    -H "Authorization: Bearer $token" `
    -F "client_turn_key=$clientTurnKey" `
    -F "audio=@$tmpWav;type=audio/wav"
$turnResp = $turnJson | ConvertFrom-Json
if (-not $turnResp.turn.id) {
    Write-Host $turnJson
    throw "Turn upload failed"
}
Write-Host "turn_id=$($turnResp.turn.id) status=$($turnResp.turn.status)"

Write-Host "== Poll events for turn.result =="
$after = 0
$found = $false
$resultPayload = $null
for ($i = 0; $i -lt 20; $i++) {
    $ev = Invoke-RestMethod -Uri "$base/accompaniment/sessions/$sessionId/events?after=$after" -Headers $headers -Method GET
    foreach ($e in $ev.events) {
        Write-Host "event: $($e.type)"
        if ($e.type -eq "turn.result") {
            $found = $true
            $resultPayload = $e.payload
        }
    }
    $after = $ev.next_after
    if ($found) { break }
    Start-Sleep -Milliseconds 200
}
if (-not $found) { throw "turn.result not received" }

Write-Host "transcript=$($resultPayload.transcript)"
Write-Host "reply=$($resultPayload.reply_text)"
$lipCount = 0
if ($resultPayload.expression -and $resultPayload.expression.lips) {
    $lipCount = @($resultPayload.expression.lips).Count
}
Write-Host "expression.version=$($resultPayload.expression.version) lips=$lipCount"

Write-Host "== Download TTS =="
$ttsPath = Join-Path $env:TEMP "empathia-phase0-tts.wav"
curl.exe -s -L "$($resultPayload.tts.url)" -H "Authorization: Bearer $token" -o $ttsPath
$ttsLen = (Get-Item $ttsPath).Length
Write-Host "tts_bytes=$ttsLen"
if ($ttsLen -lt 44) { throw "TTS file too small" }

Write-Host "== Close session =="
Invoke-RestMethod -Uri "$base/accompaniment/sessions/$sessionId/close" -Method POST -Headers $headers | Out-Null

Write-Host ""
Write-Host "PHASE 0 SMOKE OK"
Write-Host "Repo: $repo"
