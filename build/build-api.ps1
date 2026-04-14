<#
.SYNOPSIS
    Publica a API MOTORFISCALSAPB1 para execucao como Windows Service.

.DESCRIPTION
    - dotnet publish -c Release -r win-x64 --self-contained false
    - Gera pasta dist/publish/api pronta para instalacao
    - Opcionalmente instala/reinstala como Windows Service via sc.exe

    Self-contained = false assume que o Windows Server alvo tem o
    .NET 8 Runtime instalado (hosting bundle). Se preferir pacote
    auto-contido, passe -SelfContained.

.PARAMETER Install
    Se presente, cria/reinstala o servico apontando para o diretorio
    de publish. Requer rodar como Administrador.

.PARAMETER InstallPath
    Onde copiar os binarios ao instalar. Default: C:\MFSAP\Api

.PARAMETER SelfContained
    Publica pacote auto-contido (nao depende do runtime instalado).

.EXAMPLE
    pwsh ./build/build-api.ps1
    pwsh ./build/build-api.ps1 -Install
    pwsh ./build/build-api.ps1 -Install -InstallPath D:\apps\MFSAP\Api -SelfContained
#>

[CmdletBinding()]
param(
    [switch]$Install,
    [string]$InstallPath = "C:\MFSAP\Api",
    [switch]$SelfContained,
    [string]$ServiceName = "MOTORFISCALSAPB1.Api",
    [string]$DisplayName = "MOTORFISCALSAPB1 API",
    [string]$Description = "Motor fiscal inteligente para SAP Business One - HTTP API"
)

$ErrorActionPreference = "Stop"

$Root      = Split-Path -Parent $PSScriptRoot
$Project   = Join-Path $Root "src/MOTORFISCALSAPB1.Api/MOTORFISCALSAPB1.Api.csproj"
$PublishDir= Join-Path $Root "dist/publish/api"

Write-Host "==> Publish (Release, win-x64, self-contained=$SelfContained)" -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

$scFlag = if ($SelfContained) { "true" } else { "false" }
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained $scFlag `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

Write-Host ""
Write-Host "Artefato pronto em: $PublishDir" -ForegroundColor Green
Write-Host "Binario principal:  MOTORFISCALSAPB1.Api.exe" -ForegroundColor Green

if (-not $Install) {
    Write-Host ""
    Write-Host "Para instalar como Windows Service, rode novamente com -Install (como Administrador)." -ForegroundColor Yellow
    return
}

# --- Instalacao ---
Write-Host ""
Write-Host "==> Instalando como Windows Service: $ServiceName" -ForegroundColor Cyan

# Verifica Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw "Execute como Administrador para instalar o servico." }

# Para servico antigo, se existir
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "    servico existente encontrado, parando e removendo..." -ForegroundColor Yellow
    if ($existing.Status -ne 'Stopped') { Stop-Service $ServiceName -Force }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Copia binarios para InstallPath
if (-not (Test-Path $InstallPath)) { New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null }
Write-Host "    copiando binarios para $InstallPath" -ForegroundColor Cyan
Copy-Item -Path "$PublishDir\*" -Destination $InstallPath -Recurse -Force

$exePath = Join-Path $InstallPath "MOTORFISCALSAPB1.Api.exe"
if (-not (Test-Path $exePath)) { throw "$exePath nao encontrado apos copia." }

# Registra servico
Write-Host "    sc.exe create $ServiceName" -ForegroundColor Cyan
sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto DisplayName= "`"$DisplayName`"" | Out-Null
sc.exe description $ServiceName "`"$Description`"" | Out-Null

# Recovery: restart automatico em caso de falha
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/10000 | Out-Null

Write-Host ""
Write-Host "Servico $ServiceName registrado." -ForegroundColor Green
Write-Host "Inicie com:  sc.exe start $ServiceName"
Write-Host "Status:      Get-Service $ServiceName"
Write-Host "Logs:        $InstallPath\logs\"
