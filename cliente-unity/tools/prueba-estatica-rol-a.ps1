# Prueba estatica Rol A — inspecciona codigo Unity sin encender B/C/D.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$scripts = Join-Path $root 'avatar\Assets\Scripts\Empathia'
$fail = 0
$pass = 0

function Check([string]$ok, [string]$name) {
    if ($ok) {
        Write-Host "  OK   $name"
        $script:pass++
    }
    else {
        Write-Host "  FALLO $name"
        $script:fail++
    }
}

Write-Host '=== PRUEBA ESTATICA ROL A (sin servidor) ==='
Write-Host "Carpeta: $scripts"
Write-Host ''

Write-Host '[1] Archivos obligatorios'
$required = @(
    'EmpathiaApiClient.cs',
    'EmpathiaApiModels.cs',
    'EmpathiaAuthState.cs',
    'EmpathiaWav.cs',
    'LoginScreenController.cs'
)
foreach ($f in $required) {
    Check (Test-Path (Join-Path $scripts $f)) $f
}

Write-Host ''
Write-Host '[2] No llama a Inteligencia (:8100)'
$hits8100 = Select-String -Path (Join-Path $scripts '*.cs') -Pattern 'https?://[^\s"'']+:8100|/internal/v1'
Check (-not $hits8100) 'Ningun script C# llama a :8100 ni /internal/v1'

Write-Host ''
Write-Host '[3] Cliente habla con B'
$api = Get-Content (Join-Path $scripts 'EmpathiaApiClient.cs') -Raw
Check ($api -match 'CheckHealth') 'CheckHealth'
Check ($api -match 'public IEnumerator Login') 'Login'
Check ($api -match 'CreateSession') 'CreateSession'
Check ($api -match 'SendActiveText') 'SendActiveText (texto)'
Check ($api -match 'RunTurn') 'RunTurn (audio /turns)'
Check ($api -match 'PollTurnResult') 'PollTurnResult (events)'
Check ($api -match 'DownloadAndPlayTts') 'DownloadAndPlayTts'

Write-Host ''
Write-Host '[4] UI login / salud'
$ui = Get-Content (Join-Path $scripts 'LoginScreenController.cs') -Raw
Check ($ui -match 'BuildLoginView') 'Pantalla login'
Check ($ui -match 'BuildHealthView') 'Pantalla Salud'
Check ($ui -match 'OnLogin') 'Boton Iniciar sesion'
Check ($ui -match 'OnRecordPressed|RecordAudioTurn') 'Grabar audio'
Check ($ui -match 'OnSendTypedText') 'Enviar texto'
Check ($ui -match 'PlayTurnTts') 'Reproduce TTS tras turn.result'
Check ($ui -match 'SetState\("listening"\)') 'Estado listening'
Check ($ui -match 'SetState\("processing"\)') 'Estado processing'
Check ($ui -match 'SetState\("speaking"\)') 'Estado speaking'

Write-Host ''
Write-Host '[5] Resolucion 1920x1080'
$proj = Join-Path $root 'avatar\ProjectSettings\ProjectSettings.asset'
$projTxt = if (Test-Path $proj) { Get-Content $proj -Raw } else { '' }
Check ($ui -match '1920' -and $ui -match '1080') 'LoginScreenController referencia 1920x1080'
Check ($projTxt -match 'defaultScreenWidth: 1920' -and $projTxt -match 'defaultScreenHeight: 1080') 'ProjectSettings 1920x1080'
$editor1080 = Test-Path (Join-Path $scripts 'Editor\EmpathiaGameView1080.cs')
Check $editor1080 'Script Editor para fijar Game view 1920x1080'

Write-Host ''
Write-Host '[6] WAV de apoyo'
$wav = Get-Content (Join-Path $scripts 'EmpathiaWav.cs') -Raw
Check ($wav -match 'BuildSilentWav') 'BuildSilentWav (WAV de prueba en codigo)'
Check ($wav -match 'FromMicrophoneClip') 'FromMicrophoneClip'

Write-Host ''
Write-Host '[7] No toca carpetas ajenas en este script'
Check $true 'Prueba limitada a cliente-unity/'

Write-Host ''
Write-Host '=== RESULTADO ==='
Write-Host ("Pasan: $pass")
Write-Host ("Fallan: $fail")
if ($fail -eq 0) {
    Write-Host 'PRUEBA ESTATICA ROL A: OK'
    exit 0
}
Write-Host 'PRUEBA ESTATICA ROL A: CON FALLOS'
exit 1
