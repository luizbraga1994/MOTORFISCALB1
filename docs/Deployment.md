# Deploy — API e Worker como Windows Services

Os três componentes do motor são empacotados separadamente:

| Componente | Runtime | Deploy |
|---|---|---|
| `MOTORFISCALSAPB1.Addon` | .NET 10 (net10.0-windows) x86 + x64 | LightWeight Extension ZIP → SAP Server |
| `MOTORFISCALSAPB1.Api` | .NET 10 | **Windows Service** |
| `MOTORFISCALSAPB1.Worker` | .NET 10 | **Windows Service** |

Este guia cobre API e Worker. Para o Addon, ver [`Addon.md`](Addon.md).
Para o **runbook** com checklist passo-a-passo, smoke test e rollback,
ver [`Deployment-Checklist.md`](Deployment-Checklist.md).

## Pré-requisitos no servidor

- **Windows Server 2019+** (ou Windows 10/11).
- **.NET 10 Hosting Bundle** instalado (ou publicar com `-SelfContained`).
- **Sap.Data.Hana.Core.v2.1**: o pacote NuGet já traz as DLLs; não precisa
  instalar client HANA separado no servidor.
- Firewall: liberar porta **5080** (API) entre o servidor e os clientes
  SAP B1 (onde roda o Addon).
- Acesso de rede do servidor ao host HANA (porta 30015) e ao Service
  Layer (porta 50000).

## Configuração de credenciais (IMPORTANTE)

O `appsettings.json` versionado contém **placeholders**
(`__OVERRIDE_IN_appsettings.Production.json__`). **Nunca** coloque senhas
reais nele. Em produção, crie um `appsettings.Production.json` **fora do
repositório** no diretório de instalação (`C:\MFSAP\Api\` e
`C:\MFSAP\Worker\`) apenas com as chaves sensíveis:

```json
{
  "HanaDbConnection": {
    "Password": "<senha-real-SYSTEM>"
  },
  "SapServiceLayer": {
    "Password": "<senha-real-manager>"
  }
}
```

E defina `ASPNETCORE_ENVIRONMENT=Production` (ou
`DOTNET_ENVIRONMENT=Production` para o Worker) no serviço — o host mescla
automaticamente os dois arquivos.

Alternativas:
- **Variáveis de ambiente**: `HanaDbConnection__Password=...`
- **Windows Credential Manager** via custom config provider.
- **Azure KeyVault** / **AWS Secrets Manager** via provider oficial.

## Build e instalação

### API

```powershell
# Rodar como Administrador:
pwsh .\build\build-api.ps1 -Install
# Ou personalizado:
pwsh .\build\build-api.ps1 -Install `
     -InstallPath D:\apps\MFSAP\Api `
     -SelfContained
```

O script:
1. `dotnet publish -c Release -r win-x64` para `dist/publish/api`.
2. Copia binários para `C:\MFSAP\Api\` (ou `-InstallPath`).
3. `sc.exe create MOTORFISCALSAPB1.Api binPath= "..." start= auto`.
4. Configura recovery: restart após 5s, 5s, 10s (3 tentativas).

Iniciar:
```powershell
sc.exe start MOTORFISCALSAPB1.Api
Get-Service MOTORFISCALSAPB1.Api
```

Validar:
```powershell
curl http://localhost:5080/health
curl http://localhost:5080/swagger
```

### Worker

```powershell
pwsh .\build\build-worker.ps1 -Install
```

Mesmo padrão: publish → `C:\MFSAP\Worker\` → `MOTORFISCALSAPB1.Worker`
registrado com `sc.exe`.

## Estrutura de arquivos após instalação

```
C:\MFSAP\
├── Api\
│   ├── MOTORFISCALSAPB1.Api.exe
│   ├── MOTORFISCALSAPB1.Api.dll
│   ├── appsettings.json            (versionado, placeholders)
│   ├── appsettings.Production.json (NAO versionado, senhas reais)
│   ├── manifests\structure.json
│   ├── logs\api-YYYYMMDD.log
│   └── ... (DLLs do runtime)
└── Worker\
    ├── MOTORFISCALSAPB1.Worker.exe
    ├── appsettings.json
    ├── appsettings.Production.json
    └── logs\worker-YYYYMMDD.log
```

## Conta de execução

Os serviços rodam por padrão como `LocalSystem`. Para produção,
recomendado criar uma conta dedicada **gMSA** ou usuário local com
direitos mínimos:

```powershell
sc.exe config MOTORFISCALSAPB1.Api    obj= ".\mfsap-svc" password= "<senha>"
sc.exe config MOTORFISCALSAPB1.Worker obj= ".\mfsap-svc" password= "<senha>"
```

A conta precisa de:
- `Log on as a service` (secpol.msc → User Rights Assignment).
- Acesso de leitura/escrita em `C:\MFSAP\Api\logs\` e
  `C:\MFSAP\Worker\logs\`.
- Saída de rede para HANA e Service Layer.

## Monitoramento

- **Logs**: `C:\MFSAP\{Api,Worker}\logs\` (Serilog rolling file por dia).
- **Health check**: `GET http://localhost:5080/health` retorna JSON com
  status de HANA e Service Layer.
- **Event Viewer**: falhas no startup do serviço aparecem em
  `Windows Logs → Application` com source `MOTORFISCALSAPB1.Api` /
  `MOTORFISCALSAPB1.Worker`.
- **Performance**: `perfmon.exe` → adicionar contadores de .NET CLR
  apontando para os processos.

## Atualização em produção

```powershell
# Pela janela de manutenção:
sc.exe stop MOTORFISCALSAPB1.Api
sc.exe stop MOTORFISCALSAPB1.Worker

# Build + reinstall (preserva appsettings.Production.json se existir):
pwsh .\build\build-api.ps1    -Install
pwsh .\build\build-worker.ps1 -Install

sc.exe start MOTORFISCALSAPB1.Api
sc.exe start MOTORFISCALSAPB1.Worker

# Verificar:
curl http://localhost:5080/health
Get-Content C:\MFSAP\Api\logs\api-*.log -Tail 20
```

> **Atenção**: o script `-Install` faz cópia recursiva e **preserva**
> arquivos que não estão no pacote publicado, então seu
> `appsettings.Production.json` local não é apagado. Mas confira após
> a primeira atualização.

## Desinstalação

```powershell
sc.exe stop   MOTORFISCALSAPB1.Api
sc.exe delete MOTORFISCALSAPB1.Api
sc.exe stop   MOTORFISCALSAPB1.Worker
sc.exe delete MOTORFISCALSAPB1.Worker
Remove-Item C:\MFSAP -Recurse -Force
```

## Troubleshooting

**Serviço inicia mas morre em segundos**
→ Veja `Event Viewer → Application`. Tipicamente: `appsettings` ausente
ou HANA / Service Layer inacessíveis no startup.

**`HanaDbConnection:Server nao configurado`**
→ O appsettings não foi copiado, ou o ambiente
(`ASPNETCORE_ENVIRONMENT`) está apontando para um arquivo que não
existe.

**`InvalidOperationException: SapServiceLayer:BaseUrl nao configurado`**
→ Mesma causa. Valide com `Get-Content .\appsettings.json | jq`.

**Timeout no HANA sob carga**
→ Aumente `HanaDbConnection.CommandTimeout` e `MaxPoolSize`.

**Service Layer retorna 401 constantemente**
→ Session duration do SL é 30 min. Se o clock do servidor diverge do
SAP, reduza `SessionTimeoutMinutes` para 20.

**Porta 5080 em uso**
→ Defina `ASPNETCORE_URLS=http://+:5081` no serviço:
`sc.exe config MOTORFISCALSAPB1.Api depend= ""` não serve — use
`setx /m ASPNETCORE_URLS "http://+:5081"` e reinicie.
