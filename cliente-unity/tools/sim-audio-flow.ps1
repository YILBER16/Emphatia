$base = 'http://192.168.1.69:8000/api/v1'
$ErrorActionPreference = 'Stop'

Write-Host '=== 1) HEALTH ==='
$health = Invoke-RestMethod -Uri "$base/health" -Method Get
$health | ConvertTo-Json -Compress

Write-Host ''
Write-Host '=== 2) LOGIN ==='
$loginBody = '{"username":"estudiante1","password":"password"}'
$login = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
$token = $login.token
Write-Host ("user=" + $login.user.username + " token_len=" + $token.Length)
$headers = @{ Authorization = ("Bearer " + $token); Accept = 'application/json' }

Write-Host ''
Write-Host '=== 3) SESSION ==='
$sessionId = $null
try {
  $sess = Invoke-RestMethod -Uri "$base/accompaniment/sessions" -Method Post -Headers $headers -ContentType 'application/json' -Body '{"locale":"es","client":"unity"}'
  $sessionId = $sess.session.id
  Write-Host ("created=" + $sessionId)
} catch {
  Write-Host ("create failed: " + $_.Exception.Message)
  Write-Host $_.ErrorDetails.Message
}

if (-not $sessionId) {
  try {
    $active = Invoke-RestMethod -Uri "$base/accompaniment/sessions/active" -Headers $headers -Method Get
    $sessionId = $active.session.id
    Write-Host ("active=" + $sessionId)
  } catch {
    Write-Host ("active failed: " + $_.Exception.Message)
  }
}

$simText = 'Simulacion audio A: hola hola sonido 1 2 3 probando el recorrido'
$turnKey = [guid]::NewGuid().ToString()
$payload = @{ text = $simText; message = $simText; client_turn_key = $turnKey } | ConvertTo-Json
Write-Host ''
Write-Host '=== 4) POST /active/text ==='
Write-Host ("text=" + $simText)
$post = Invoke-RestMethod -Uri "$base/accompaniment/sessions/active/text" -Method Post -Headers $headers -ContentType 'application/json' -Body $payload
$post | ConvertTo-Json -Depth 6
$turnId = $post.turn.id
if (-not $sessionId) { $sessionId = $post.session_id }
Write-Host ("turnId=" + $turnId)
Write-Host ("sessionId=" + $sessionId)

Write-Host ''
Write-Host '=== 5) POLL /events ==='
$after = 0
$found = $null
$deadline = (Get-Date).AddSeconds(45)
while ((Get-Date) -lt $deadline) {
  $evUrl = "$base/accompaniment/sessions/$sessionId/events?after=$after"
  $page = Invoke-RestMethod -Uri $evUrl -Headers $headers -Method Get
  $after = $page.next_after
  foreach ($ev in @($page.events)) {
    if ($null -eq $ev) { continue }
    Write-Host ("event id=" + $ev.id + " type=" + $ev.type)
    $matchTurn = (-not $turnId) -or ($ev.payload.turn_id -eq $turnId)
    if ($matchTurn -and ($ev.type -eq 'turn.result' -or $ev.type -eq 'turn.error')) {
      $found = $ev
      break
    }
  }
  if ($found) { break }
  Start-Sleep -Milliseconds 500
}

Write-Host ''
Write-Host '=== 6) RESULTADO ==='
if ($null -eq $found) {
  Write-Host 'FAIL: timeout sin turn.result'
  exit 1
}
Write-Host ("type=" + $found.type)
Write-Host ("transcript=" + $found.payload.transcript)
Write-Host ("reply_text=" + $found.payload.reply_text)
$found.payload | ConvertTo-Json -Depth 5
if ($found.type -eq 'turn.result' -and $found.payload.reply_text) {
  Write-Host 'OK: recorrido A to B to events correcto'
  exit 0
}
Write-Host 'FAIL: evento sin reply util'
exit 1
