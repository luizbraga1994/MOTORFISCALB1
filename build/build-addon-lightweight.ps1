<#
.SYNOPSIS
    Empacota o addon MOTORFISCALSAPB1 como LightWeight (ZIP) pronto para
    registro em Administration -> Add-Ons -> Add-On Administration.

.DESCRIPTION
    - Compila MOTORFISCALSAPB1.Addon em Release x86 (.NET Framework 4.8).
    - Coleta o .exe + .config + todas as DLLs de dependencia em uma unica
      pasta publish.
    - Remove SAPbouiCOM.dll e SAPbobsCOM.dll do pacote (fornecidas pelo
      Client do SAP B1; nao devem ser redistribuidas).
    - Gera ZIP: MOTORFISCALSAPB1.Addon-<versao>-x86.zip em ./dist/.

.PARAMETER Version
    Versao do addon. Default: le do .csproj (ou 1.0.0).

.PARAMETER Configuration
    Release (default) ou Debug.

.EXAMPLE
    pwsh ./build/build-addon-lightweight.ps1 -Version 1.0.0
#>

[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [ValidateSet("Release","Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root      = Split-Path -Parent $PSScriptRoot
$Project   = Join-Path $Root "src/MOTORFISCALSAPB1.Addon/MOTORFISCALSAPB1.Addon.csproj"
$OutDir    = Join-Path $Root "src/MOTORFISCALSAPB1.Addon/bin/x86/$Configuration"
$PublishDir= Join-Path $Root "dist/publish/MOTORFISCALSAPB1.Addon"
$DistDir   = Join-Path $Root "dist"
$ZipName   = "MOTORFISCALSAPB1.Addon-$Version-x86.zip"
$ZipPath   = Join-Path $DistDir $ZipName

Write-Host "==> Restaurando e compilando (x86 / $Configuration)..." -ForegroundColor Cyan
dotnet restore $Project /p:Platform=x86
if ($LASTEXITCODE -ne 0) { throw "dotnet restore falhou." }

dotnet build $Project -c $Configuration /p:Platform=x86 --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build falhou." }

Write-Host "==> Coletando artefatos em $PublishDir" -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Get-ChildItem -Path $OutDir -File | ForEach-Object {
    Copy-Item $_.FullName -Destination $PublishDir
}

# Remove DLLs da instalacao SAP (nao redistribuimos).
$SapAssemblies = @("SAPbouiCOM.dll", "SAPbobsCOM.dll",
                   "Interop.SAPbouiCOM.dll", "Interop.SAPbobsCOM.dll")
foreach ($asm in $SapAssemblies) {
    $p = Join-Path $PublishDir $asm
    if (Test-Path $p) {
        Write-Host "    removendo $asm (fornecida pelo SAP B1 Client)" -ForegroundColor Yellow
        Remove-Item $p -Force
    }
}

# Remove pdbs e arquivos de documentacao para reduzir tamanho.
Get-ChildItem -Path $PublishDir -Include *.pdb,*.xml -Recurse |
    Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "==> Gerando manifesto addon.manifest.json" -ForegroundColor Cyan
$manifest = [ordered]@{
    name         = "MOTORFISCALSAPB1"
    version      = $Version
    platform     = "x86"
    framework    = "net48"
    deployment   = "LightWeight"
    entryPoint   = "MOTORFISCALSAPB1.Addon.exe"
    description  = "Motor fiscal inteligente para SAP Business One"
    publisher    = "MOTORFISCALSAPB1"
    builtUtc     = (Get-Date).ToUniversalTime().ToString("o")
}
$manifest | ConvertTo-Json -Depth 5 |
    Out-File -FilePath (Join-Path $PublishDir "addon.manifest.json") -Encoding UTF8

Write-Host "==> Empacotando $ZipPath" -ForegroundColor Cyan
if (-not (Test-Path $DistDir)) { New-Item -ItemType Directory -Path $DistDir | Out-Null }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal

$size = [math]::Round((Get-Item $ZipPath).Length / 1KB, 1)
Write-Host ""
Write-Host "Pacote LightWeight gerado:" -ForegroundColor Green
Write-Host "   $ZipPath  ($size KB)"
Write-Host ""
Write-Host "Proximo passo: no SAP B1, abra"
Write-Host "   Administration -> Add-Ons -> Add-On Administration"
Write-Host "selecione 'Simple Install / Light Deployment' e aponte para o ZIP acima."
