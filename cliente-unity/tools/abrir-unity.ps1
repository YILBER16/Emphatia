# Abre el cliente Unity desde D: (copia actual). No uses C:\Emphatia.
$project = 'D:\Emphatia\cliente-unity\avatar'
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'

if (-not (Test-Path $project)) {
    Write-Host "No existe el proyecto: $project"
    exit 1
}
if (-not (Test-Path $editor)) {
    Write-Host "No existe Unity Editor: $editor"
    exit 1
}

Write-Host "Abriendo Unity 6 con: $project"
Start-Process -FilePath $editor -ArgumentList "-projectPath `"$project`""
