<#
.SYNOPSIS
    Cria os 4 appsettings.{Development,Production}.json (API + Worker)
    que sao gitignored e nao vem via git pull.

.DESCRIPTION
    Gera os arquivos idempotentemente. Se o arquivo ja existir, PULA
    (nao sobrescreve — assim suas senhas editadas nao sao perdidas).

    Duas modalidades:

    1. Default (sem -InstallPath) — cria em src\MOTORFISCALSAPB1.*\ para
       debug local via dotnet run / F5 no Visual Studio.

    2. Com -InstallPath — cria em <path>\Api\ e <path>\Worker\ para o
       servidor de producao (ex: -InstallPath C:\MFSAP).

    Os valores de Development vem pre-preenchidos com o ambiente de
    desenvolvimento conhecido (saphaalbieri / SBODEMOBR). Os de
    Production sao TEMPLATES com placeholders "TROCAR_*" que VOCE
    precisa editar antes de iniciar o servico.

.PARAMETER InstallPath
    Raiz para criar <path>\Api\ e <path>\Worker\. Se omitido, cria em
    src\MOTORFISCALSAPB1.{Api,Worker}\ do repositorio.

.PARAMETER Force
    Sobrescreve arquivos existentes. CUIDADO: vai zerar senhas editadas.

.EXAMPLE
    pwsh .\build\setup-local-secrets.ps1
    # Debug local — cria em src\*\

.EXAMPLE
    pwsh .\build\setup-local-secrets.ps1 -InstallPath C:\MFSAP
    # Servidor — cria em C:\MFSAP\Api\ e C:\MFSAP\Worker\

.EXAMPLE
    pwsh .\build\setup-local-secrets.ps1 -Force
    # Recria tudo do zero.
#>
[CmdletBinding()]
param(
    [string]$InstallPath = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# --- conteudo dos 4 arquivos ---------------------------------------------

$apiDevelopment = @'
{
  "//": "appsettings.Development.json — GITIGNORED. Nao commitar.",
  "//2": "Ativo automaticamente quando ASPNETCORE_ENVIRONMENT=Development (default em dotnet run / F5).",

  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "MOTORFISCALSAPB1": "Debug"
    }
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/api-dev-.log", "rollingInterval": "Day" } }
    ]
  },

  "HanaDbConnection": {
    "Server": "saphaalbieri:30015",
    "Port": "30015",
    "Database": "SBODEMOBR",
    "UserID": "SYSTEM",
    "Password": "#7reGT#15OyUUa!",
    "MaxPoolSize": 100,
    "MinPoolSize": 10,
    "ConnectionTimeout": 60,
    "CommandTimeout": 300
  },

  "SapServiceLayer": {
    "BaseUrl": "https://saphaalbieri:50000",
    "CompanyDB": "SBODEMOBR",
    "UserName": "manager",
    "Password": "B1@Albieri",
    "Language": 29,
    "IgnoreSslErrors": true,
    "TimeoutSeconds": 180,
    "MaxRetryAttempts": 3,
    "SessionTimeoutMinutes": 25
  },

  "Structure": {
    "ManifestPath": "manifests/structure.json"
  }
}
'@

$apiProduction = @'
{
  "//": "appsettings.Production.json — GITIGNORED. Preencher no servidor de producao.",
  "//2": "Ativo quando ASPNETCORE_ENVIRONMENT=Production (default quando nada esta setado).",
  "//3": "Mescla com appsettings.json — aqui so' vai o que difere (credenciais, hosts).",

  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/api-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } }
    ]
  },

  "HanaDbConnection": {
    "Server": "TROCAR_hana-host:30015",
    "Port": "30015",
    "Database": "TROCAR_SBO_COMP",
    "UserID": "TROCAR_SBO_USER",
    "Password": "TROCAR_SENHA_HANA",
    "MaxPoolSize": 100,
    "MinPoolSize": 10,
    "ConnectionTimeout": 60,
    "CommandTimeout": 300
  },

  "SapServiceLayer": {
    "BaseUrl": "TROCAR_https://sap-host:50000",
    "CompanyDB": "TROCAR_SBO_COMP",
    "UserName": "TROCAR_usuario-sl",
    "Password": "TROCAR_SENHA_SL",
    "Language": 29,
    "IgnoreSslErrors": false,
    "TimeoutSeconds": 180,
    "MaxRetryAttempts": 3,
    "SessionTimeoutMinutes": 25
  }
}
'@

$workerDevelopment = @'
{
  "//": "appsettings.Development.json — GITIGNORED. Nao commitar.",
  "//2": "Ativo automaticamente quando DOTNET_ENVIRONMENT=Development.",

  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "MOTORFISCALSAPB1": "Debug"
    }
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/worker-dev-.log", "rollingInterval": "Day" } }
    ]
  },
  "Worker": {
    "RuleCacheRefreshMinutes": 1
  },

  "HanaDbConnection": {
    "Server": "saphaalbieri:30015",
    "Port": "30015",
    "Database": "SBODEMOBR",
    "UserID": "SYSTEM",
    "Password": "#7reGT#15OyUUa!",
    "MaxPoolSize": 100,
    "MinPoolSize": 10,
    "ConnectionTimeout": 60,
    "CommandTimeout": 300
  },

  "SapServiceLayer": {
    "BaseUrl": "https://saphaalbieri:50000",
    "CompanyDB": "SBODEMOBR",
    "UserName": "manager",
    "Password": "B1@Albieri",
    "Language": 29,
    "IgnoreSslErrors": true,
    "TimeoutSeconds": 180,
    "MaxRetryAttempts": 3,
    "SessionTimeoutMinutes": 25
  }
}
'@

$workerProduction = @'
{
  "//": "appsettings.Production.json — GITIGNORED. Preencher no servidor de producao.",
  "//2": "Ativo quando DOTNET_ENVIRONMENT=Production (default quando nada esta setado).",
  "//3": "Mescla com appsettings.json — aqui so' vai o que difere (credenciais, hosts).",

  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/worker-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } }
    ]
  },
  "Worker": {
    "RuleCacheRefreshMinutes": 5
  },

  "HanaDbConnection": {
    "Server": "TROCAR_hana-host:30015",
    "Port": "30015",
    "Database": "TROCAR_SBO_COMP",
    "UserID": "TROCAR_SBO_USER",
    "Password": "TROCAR_SENHA_HANA",
    "MaxPoolSize": 100,
    "MinPoolSize": 10,
    "ConnectionTimeout": 60,
    "CommandTimeout": 300
  },

  "SapServiceLayer": {
    "BaseUrl": "TROCAR_https://sap-host:50000",
    "CompanyDB": "TROCAR_SBO_COMP",
    "UserName": "TROCAR_usuario-sl",
    "Password": "TROCAR_SENHA_SL",
    "Language": 29,
    "IgnoreSslErrors": false,
    "TimeoutSeconds": 180,
    "MaxRetryAttempts": 3,
    "SessionTimeoutMinutes": 25
  }
}
'@

# --- determinar destino --------------------------------------------------

if ([string]::IsNullOrWhiteSpace($InstallPath)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $apiDir    = Join-Path $repoRoot 'src\MOTORFISCALSAPB1.Api'
    $workerDir = Join-Path $repoRoot 'src\MOTORFISCALSAPB1.Worker'
    Write-Host "Modo DEV LOCAL — destinos:" -ForegroundColor Cyan
} else {
    $apiDir    = Join-Path $InstallPath 'Api'
    $workerDir = Join-Path $InstallPath 'Worker'
    Write-Host "Modo SERVIDOR — destinos:" -ForegroundColor Cyan
}

Write-Host "  API   : $apiDir"
Write-Host "  Worker: $workerDir"
Write-Host ""

# --- helper de escrita idempotente --------------------------------------

function Write-ConfigFile {
    param(
        [string]$Path,
        [string]$Content,
        [string]$Label
    )
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    if ((Test-Path $Path) -and -not $Force) {
        Write-Host "  [SKIP] $Label ja existe — preservado." -ForegroundColor Yellow
        Write-Host "         $Path"
        return
    }

    # Set-Content sem BOM (UTF8NoBOM em PS 6+; fallback manual em 5.1):
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        Set-Content -Path $Path -Value $Content -Encoding utf8NoBOM
    } else {
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
    }
    Write-Host "  [OK]   $Label criado." -ForegroundColor Green
    Write-Host "         $Path"
}

# --- escrever os 4 arquivos ---------------------------------------------

Write-ConfigFile -Path (Join-Path $apiDir    'appsettings.Development.json') -Content $apiDevelopment    -Label 'API Development   '
Write-ConfigFile -Path (Join-Path $apiDir    'appsettings.Production.json')  -Content $apiProduction     -Label 'API Production    '
Write-ConfigFile -Path (Join-Path $workerDir 'appsettings.Development.json') -Content $workerDevelopment -Label 'Worker Development'
Write-ConfigFile -Path (Join-Path $workerDir 'appsettings.Production.json')  -Content $workerProduction  -Label 'Worker Production '

Write-Host ""
if ([string]::IsNullOrWhiteSpace($InstallPath)) {
    Write-Host "Pronto para debug local. Proximos passos:" -ForegroundColor Cyan
    Write-Host "  cd src\MOTORFISCALSAPB1.Api;    `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run"
    Write-Host "  cd src\MOTORFISCALSAPB1.Worker; `$env:DOTNET_ENVIRONMENT   ='Development'; dotnet run"
} else {
    Write-Host "Pronto para producao. AGORA:" -ForegroundColor Cyan
    Write-Host "  1) Edite os Production.json trocando TROCAR_* por valores reais:"
    Write-Host "     notepad `"$apiDir\appsettings.Production.json`""
    Write-Host "     notepad `"$workerDir\appsettings.Production.json`""
    Write-Host "  2) sc.exe start MOTORFISCALSAPB1.Api"
    Write-Host "     sc.exe start MOTORFISCALSAPB1.Worker"
}
