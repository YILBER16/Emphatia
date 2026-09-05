param(
    [string]$BaseUrl = "http://127.0.0.1:8000/api/v1"
)

$ErrorActionPreference = "Stop"
Write-Host "=== EmpathIA demo perfiles ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"

function Split-Http {
    param([string]$Raw)
    $marker = "__HTTP__"
    $idx = $Raw.LastIndexOf($marker)
    if ($idx -lt 0) {
        return @{ Body = $Raw; Code = 0 }
    }
    $body = $Raw.Substring(0, $idx).Trim()
    $codeText = $Raw.Substring($idx + $marker.Length).Trim()
    return @{ Body = $body; Code = [int]$codeText }
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Token = "",
        [string]$JsonBody = $null
    )
    $args = @("-s", "-w", "`n__HTTP__%{http_code}", "-X", $Method, $Url, "-H", "Accept: application/json")
    if ($Token) {
        $args += @("-H", "Authorization: Bearer $Token")
    }
    $tmp = $null
    if ($null -ne $JsonBody) {
        $tmp = Join-Path $env:TEMP ("empathia_" + [guid]::NewGuid().ToString("N") + ".json")
        [System.IO.File]::WriteAllText($tmp, $JsonBody, [System.Text.UTF8Encoding]::new($false))
        $args += @("-H", "Content-Type: application/json", "--data-binary", "@$tmp")
    }
    try {
        $raw = & curl.exe @args
        return (Split-Http ($raw -join "`n"))
    } finally {
        if ($tmp) { Remove-Item $tmp -ErrorAction SilentlyContinue }
    }
}

$health = Invoke-Api -Method GET -Url "$BaseUrl/health"
if ($health.Code -ne 200) { throw "Health fallo HTTP $($health.Code): $($health.Body)" }
Write-Host "OK health" -ForegroundColor Green

$admin = Invoke-Api -Method POST -Url "$BaseUrl/auth/login" -JsonBody '{"username":"admin1","password":"password"}'
if ($admin.Code -ne 200) { throw "Admin login fallo: $($admin.Body)" }
$adminObj = $admin.Body | ConvertFrom-Json
$adminToken = $adminObj.token
Write-Host "OK admin login ($($adminObj.user.role))" -ForegroundColor Green

$doc = "3" + (Get-Random -Minimum 100000000 -Maximum 999999999).ToString()
$createObj = [ordered]@{
    nombres = "Lucia"
    apellidos = "Demo Fase5"
    nombre_preferencia = "Luci"
    grado = "7-2"
    edad = 13
    sede = "Sede Lab"
    jornada = "manana"
    documento_numero = $doc
    acudiente_telefono = "3009998877"
    acudiente_documento = "80011223"
}
$createBody = $createObj | ConvertTo-Json -Compress
$create = Invoke-Api -Method POST -Url "$BaseUrl/admin/students" -Token $adminToken -JsonBody $createBody
if ($create.Code -ne 201) { throw "Create student fallo HTTP $($create.Code): $($create.Body)" }
$created = $create.Body | ConvertFrom-Json
$profileId = $created.data.id
$userId = $created.data.user_id
$code1 = $created.data.access_code
Write-Host "OK create profile id=$profileId user_id=$userId access_code=$code1" -ForegroundColor Green

$regen = Invoke-Api -Method POST -Url "$BaseUrl/admin/students/$profileId/regenerate-code" -Token $adminToken
if ($regen.Code -ne 200) { throw "Regenerate fallo: $($regen.Body)" }
$regenObj = $regen.Body | ConvertFrom-Json
$code2 = $regenObj.data.access_code
if ($code2 -eq $code1) { throw "access_code no cambio al regenerar" }
Write-Host "OK regenerate access_code=$code2" -ForegroundColor Green

$counselor = Invoke-Api -Method POST -Url "$BaseUrl/auth/login" -JsonBody '{"username":"orientador1","password":"password"}'
if ($counselor.Code -ne 200) { throw "Counselor login fallo: $($counselor.Body)" }
$cObj = $counselor.Body | ConvertFrom-Json
$cToken = $cObj.token

$list = Invoke-Api -Method GET -Url "$BaseUrl/students" -Token $cToken
if ($list.Code -ne 200) { throw "List students fallo: $($list.Body)" }
$listObj = $list.Body | ConvertFrom-Json
$found = $listObj.data | Where-Object { $_.id -eq "$userId" }
if (-not $found) { throw "El estudiante creado no aparece en GET /students" }
if ($null -ne $found.PSObject.Properties["access_code"] -and $null -ne $found.access_code) {
    throw "GET /students no debe exponer access_code"
}
Write-Host "OK list students count=$($listObj.data.Count) includes Luci" -ForegroundColor Green

$assume = Invoke-Api -Method POST -Url "$BaseUrl/students/$userId/assume" -Token $cToken
if ($assume.Code -ne 200) { throw "Assume fallo: $($assume.Body)" }
$assumed = $assume.Body | ConvertFrom-Json
$stuToken = $assumed.token
if ($assumed.user.role -ne "student") { throw "Assume no devolvio role=student" }
Write-Host "OK assume as $($assumed.user.display_name)" -ForegroundColor Green

$sess = Invoke-Api -Method POST -Url "$BaseUrl/accompaniment/sessions" -Token $stuToken -JsonBody '{}'
if ($sess.Code -notin 200, 201) { throw "Create session fallo: $($sess.Body)" }
$sessObj = $sess.Body | ConvertFrom-Json
$sessionId = $sessObj.session.id
Write-Host "OK session $sessionId" -ForegroundColor Green

$turnKey = [guid]::NewGuid().ToString()
$textObj = [ordered]@{
    text = "Hola, soy Luci en la demo fase 5"
    client_turn_key = $turnKey
}
$textBody = $textObj | ConvertTo-Json -Compress
$text = Invoke-Api -Method POST -Url "$BaseUrl/accompaniment/sessions/active/text" -Token $stuToken -JsonBody $textBody
if ($text.Code -notin 200, 202) { throw "Text turn fallo HTTP $($text.Code): $($text.Body)" }
Write-Host "OK text turn accepted" -ForegroundColor Green

$after = 0
$gotResult = $false
$reply = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 500
    $ev = Invoke-Api -Method GET -Url "$BaseUrl/accompaniment/sessions/$sessionId/events?after=$after" -Token $stuToken
    if ($ev.Code -ne 200) { throw "Events fallo: $($ev.Body)" }
    $evObj = $ev.Body | ConvertFrom-Json
    if ($evObj.next_after) { $after = $evObj.next_after }
    foreach ($e in @($evObj.events)) {
        if ($e.type -eq "turn.result") {
            $gotResult = $true
            $reply = $e.payload.reply_text
            break
        }
        if ($e.type -eq "turn.error") {
            throw "turn.error: $($e.payload.message)"
        }
    }
    if ($gotResult) { break }
}

if (-not $gotResult) { throw "Timeout esperando turn.result" }
Write-Host "OK turn.result reply=$reply" -ForegroundColor Green
Write-Host ""
Write-Host "PHASE 5 DEMO OK" -ForegroundColor Cyan
Write-Host "Unity: login orientador1 -> elegir Luci (u otro activo) -> texto."
