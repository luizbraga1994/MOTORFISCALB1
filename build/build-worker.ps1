<#
.SYNOPSIS
    Publica o Worker MOTORFISCALSAPB1 para execucao como Windows Service.

.EXAMPLE
    pwsh ./build/build-worker.ps1
    pwsh ./build/build-worker.ps1 -Install
    pwsh ./build/build-worker.ps1 -Install -InstallPath D:\apps\MFSAP\Worker -SelfContained
#>

[CmdletBinding()]
param(
    [switch]$Install,
    [string]$InstallPath = "C:\MFSAP\Worker",
    [switch]$SelfContained,
    [string]$ServiceName = "MOTORFISCALSAPB1.Worker",
    [string]$DisplayName = "MOTORFISCALSAPB1 Worker",
    [string]$Description = "Motor fiscal inteligente para SAP Business One - Background Worker (rule cache refresh)"
)

$ErrorActionPreference = "Stop"

$Root      = Split-Path -Parent $PSScriptRoot
$Project   = Join-Path $Root "src/MOTORFISCALSAPB1.Worker/MOTORFISCALSAPB1.Worker.csproj"
$PublishDir= Join-Path $Root "dist/publish/worker"

Write-Host "==> Publish (Release, win-x64, self-contained=$SelfContained)" -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

$scFlag = if ($SelfContained) { "true" } else { "false" }
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained $scFlag `
    -p:PublishSingleFile=false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

Write-Host "Artefato pronto em: $PublishDir" -ForegroundColor Green

if (-not $Install) {
    Write-Host "Para instalar, rode novamente com -Install (como Administrador)." -ForegroundColor Yellow
    return
}

Write-Host ""
Write-Host "==> Instalando como Windows Service: $ServiceName" -ForegroundColor Cyan

$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw "Execute como Administrador." }

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -ne 'Stopped') { Stop-Service $ServiceName -Force }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

if (-not (Test-Path $InstallPath)) { New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null }
Copy-Item -Path "$PublishDir\*" -Destination $InstallPath -Recurse -Force

$exePath = Join-Path $InstallPath "MOTORFISCALSAPB1.Worker.exe"
if (-not (Test-Path $exePath)) { throw "$exePath nao encontrado." }

sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto DisplayName= "`"$DisplayName`"" | Out-Null
sc.exe description $ServiceName "`"$Description`"" | Out-Null
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/10000 | Out-Null

Write-Host ""
Write-Host "Servico $ServiceName registrado." -ForegroundColor Green
Write-Host "Inicie com:  sc.exe start $ServiceName"
