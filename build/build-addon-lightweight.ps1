<#
.SYNOPSIS
    Empacota o addon MOTORFISCALSAPB1 como LightWeight (ZIP) pronto para
    registro em Administration -> Add-Ons -> Add-On Administration.

    Gera DOIS pacotes — um x86 e um x64 — porque as COM DLLs do SAP B1
    Client (SAPbouiCOM/SAPbobsCOM) sao bitness-specificas. Na hora de
    instalar, o admin escolhe o ZIP correspondente ao bitness do Client.

.DESCRIPTION
    Para cada bitness (x86, x64):
      - dotnet publish -c Release -r win-<arch> --self-contained true
        (embarca o runtime .NET 10 dentro do pacote; elimina pre-requisito
        nos clients SAP B1, ao custo de ~60MB por ZIP).
      - Remove SAPbouiCOM.dll/SAPbobsCOM.dll (fornecidas pelo Client SAP).
      - Gera manifest addon.manifest.json.
      - Gera ZIP: MOTORFISCALSAPB1.Addon-<versao>-<arch>.zip em ./dist/.

.PARAMETER Version
    Versao do addon. Default: 1.0.0.

.PARAMETER Configuration
    Release (default) ou Debug.

.PARAMETER Platform
    Bitness a empacotar: x86, x64 ou both (default).

.PARAMETER FrameworkDependent
    Se presente, publica framework-dependent (~5MB, mas exige .NET 10
    Desktop Runtime x86/x64 instalado nos clients).

.EXAMPLE
    pwsh ./build/build-addon-lightweight.ps1
    # Gera ambos os ZIPs, self-contained.

.EXAMPLE
    pwsh ./build/build-addon-lightweight.ps1 -Platform x64
    # So x64.

.EXAMPLE
    pwsh ./build/build-addon-lightweight.ps1 -FrameworkDependent
    # ZIPs pequenos, mas exigem .NET 10 no client.
#>

[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [ValidateSet("Release","Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("x86","x64","both")]
    [string]$Platform = "both",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$Root     = Split-Path -Parent $PSScriptRoot
$Project  = Join-Path $Root "src/MOTORFISCALSAPB1.Addon/MOTORFISCALSAPB1.Addon.csproj"
$DistDir  = Join-Path $Root "dist"
if (-not (Test-Path $DistDir)) { New-Item -ItemType Directory -Path $DistDir | Out-Null }

$archList = if ($Platform -eq "both") { @("x86","x64") } else { @($Platform) }
$selfContained = -not $FrameworkDependent

$SapAssemblies = @(
    "SAPbouiCOM.dll", "SAPbobsCOM.dll",
    "Interop.SAPbouiCOM.dll", "Interop.SAPbobsCOM.dll"
)

foreach ($arch in $archList) {
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Cyan
    Write-Host " Publicando addon $arch ($Configuration, self-contained=$selfContained)" -ForegroundColor Cyan
    Write-Host "=================================================" -ForegroundColor Cyan

    $rid        = "win-$arch"
    $PublishDir = Join-Path $Root "dist/publish/addon/$arch"
    $ZipName    = "MOTORFISCALSAPB1.Addon-$Version-$arch.zip"
    $ZipPath    = Join-Path $DistDir $ZipName

    if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

    $scFlag = if ($selfContained) { "true" } else { "false" }

    dotnet publish $Project `
        -c $Configuration `
        -r $rid `
        -p:Platform=$arch `
        --self-contained $scFlag `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou para $arch." }

    # Remove DLLs da instalacao SAP (nao redistribuimos).
    foreach ($asm in $SapAssemblies) {
        $p = Join-Path $PublishDir $asm
        if (Test-Path $p) {
            Write-Host "    removendo $asm (fornecida pelo SAP B1 Client)" -ForegroundColor Yellow
            Remove-Item $p -Force
        }
    }

    # Remove pdbs e xmls de doc para reduzir tamanho (mantem .deps/.runtimeconfig).
    Get-ChildItem -Path $PublishDir -Include *.pdb,*.xml -Recurse -File |
        Remove-Item -Force -ErrorAction SilentlyContinue

    # Manifest
    $manifest = [ordered]@{
        name         = "MOTORFISCALSAPB1"
        version      = $Version
        platform     = $arch
        framework    = "net10.0-windows"
        runtime      = if ($selfContained) { "self-contained ($rid)" } else { "framework-dependent (.NET 10 Desktop Runtime $arch)" }
        deployment   = "LightWeight"
        entryPoint   = "MOTORFISCALSAPB1.Addon.exe"
        description  = "Motor fiscal inteligente para SAP Business One"
        publisher    = "MOTORFISCALSAPB1"
        builtUtc     = (Get-Date).ToUniversalTime().ToString("o")
    }
    $manifest | ConvertTo-Json -Depth 5 |
        Out-File -FilePath (Join-Path $PublishDir "addon.manifest.json") -Encoding UTF8

    # Zip
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal

    $size = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
    Write-Host ""
    Write-Host "  $ZipName  ($size MB)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Pacotes LightWeight gerados em $DistDir" -ForegroundColor Green
Write-Host ""
Write-Host "Proximo passo: no SAP B1 Server Manager -> Extension Manager," -ForegroundColor Cyan
Write-Host "faca upload do ZIP que bate com o bitness do SAP B1 Client instalado:"
Write-Host "  - Client 32-bit -> MOTORFISCALSAPB1.Addon-$Version-x86.zip"
Write-Host "  - Client 64-bit -> MOTORFISCALSAPB1.Addon-$Version-x64.zip"
