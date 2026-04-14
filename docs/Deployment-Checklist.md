# Runbook de Deploy — MOTORFISCALSAPB1

Checklist operacional, passo-a-passo, para colocar os 3 componentes em pé.
Objetivo: **zero incerteza** na hora de rodar em produção. Cada passo tem
comando copiável e critério de sucesso.

> **Pré-requisito absoluto**: PowerShell **como Administrador** em tudo
> que envolve `sc.exe`, `setx /m` e `SetEnvironmentVariable("Machine")`.
> 90% dos erros reportados (`Acesso ao Registro não permitido`,
> `serviço não existe`) são esse detalhe.

---

## Parte 1 — Debug local (VS2022 / F5)

> Executar **uma única vez** na máquina de desenvolvimento.

### 1.1 Criar os 4 `appsettings.{Development,Production}.json`

Os arquivos são **gitignored** — não vêm via `git pull`. Rode este
script na raiz do repositório (`D:\ProjetosGit\MOTORFISCALB1`):

```powershell
pwsh .\build\setup-local-secrets.ps1
```

Critério de sucesso: listar os 4 arquivos:

```powershell
Get-ChildItem src\MOTORFISCALSAPB1.*\appsettings.*.json |
  Select-Object FullName, Length
```

Deve retornar:

- `src\MOTORFISCALSAPB1.Api\appsettings.json`
- `src\MOTORFISCALSAPB1.Api\appsettings.Development.json`
- `src\MOTORFISCALSAPB1.Api\appsettings.Production.json`
- `src\MOTORFISCALSAPB1.Worker\appsettings.json`
- `src\MOTORFISCALSAPB1.Worker\appsettings.Development.json`
- `src\MOTORFISCALSAPB1.Worker\appsettings.Production.json`

### 1.2 Rodar API em Debug

```powershell
cd src\MOTORFISCALSAPB1.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

Ou, no VS2022: abrir `MOTORFISCALSAPB1.sln`, set `MOTORFISCALSAPB1.Api`
como startup project, **F5**.

Critério: `curl http://localhost:5080/health` retorna `200 OK`.

### 1.3 Rodar Worker em Debug

```powershell
cd src\MOTORFISCALSAPB1.Worker
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run
```

Critério: log mostra `Rule cache refreshed — N rules loaded`.

### 1.4 Rodar Addon em Debug (VS2022)

Ver [`Debug.md`](Debug.md). Resumo:

1. Abrir VS2022 **como Administrador**.
2. Set `MOTORFISCALSAPB1.Addon` como startup.
3. Plataforma **x86**.
4. F5.

Critério: o ícone do addon aparece em *SAP B1 Client → Administração →
Add-Ons → Administração de Add-On → Add-Ons em execução*.

---

## Parte 2 — Deploy de produção (servidor Windows)

> Executar na **primeira vez** no servidor de produção.

### 2.1 Pré-requisitos

```powershell
# PowerShell como Administrador
# Confirmar runtime .NET 8
dotnet --list-runtimes | Select-String "Microsoft.NETCore.App 8"
dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8"
```

Se não aparecer, baixar o **.NET 8 Hosting Bundle**:
<https://dotnet.microsoft.com/download/dotnet/8.0>

Firewall (apenas API):

```powershell
New-NetFirewallRule -DisplayName "MOTORFISCALSAPB1 API" `
  -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow
```

### 2.2 Clonar / atualizar o repositório

```powershell
cd D:\ProjetosGit
git clone https://github.com/luizbraga1994/motorfiscalb1.git MOTORFISCALB1
# ou, se já existe:
cd D:\ProjetosGit\MOTORFISCALB1
git fetch origin
git checkout main
git pull origin main
```

### 2.3 Publicar e instalar a API

```powershell
# PowerShell como Administrador
cd D:\ProjetosGit\MOTORFISCALB1
pwsh .\build\build-api.ps1 -Install
```

Critério:

```powershell
Get-Service MOTORFISCALSAPB1.Api
# Status   Name                DisplayName
# ------   ----                -----------
# Stopped  MOTORFISCALSAPB1... MOTORFISCALSAPB1 API
```

### 2.4 Publicar e instalar o Worker

```powershell
pwsh .\build\build-worker.ps1 -Install
```

Critério: `Get-Service MOTORFISCALSAPB1.Worker` retorna `Stopped`.

### 2.5 Configurar ambiente e credenciais

```powershell
# PowerShell como Administrador
[Environment]::SetEnvironmentVariable(
  "ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable(
  "DOTNET_ENVIRONMENT",     "Production", "Machine")
```

Copiar (ou gerar no servidor) os `appsettings.Production.json` com
**credenciais reais**:

```powershell
# Se ainda não existe — gerar template e editar manualmente:
pwsh .\build\setup-local-secrets.ps1 -InstallPath C:\MFSAP

# Editar as senhas:
notepad C:\MFSAP\Api\appsettings.Production.json
notepad C:\MFSAP\Worker\appsettings.Production.json
```

Campos **obrigatórios** a preencher (substituir os `TROCAR_*`):

- `HanaDbConnection.Server` — ex: `sap-hana-prod.empresa.local:30015`
- `HanaDbConnection.Database` — ex: `SBO_PROD`
- `HanaDbConnection.UserID` — ex: `MFSAP_SVC`
- `HanaDbConnection.Password`
- `SapServiceLayer.BaseUrl` — ex: `https://sap-b1-prod.empresa.local:50000`
- `SapServiceLayer.CompanyDB` — mesmo valor de `HanaDbConnection.Database`
- `SapServiceLayer.UserName`
- `SapServiceLayer.Password`

### 2.6 Iniciar os serviços

```powershell
sc.exe start MOTORFISCALSAPB1.Api
sc.exe start MOTORFISCALSAPB1.Worker
```

Critério de sucesso — **tudo isto deve passar**:

```powershell
# API responde
curl http://localhost:5080/health
# Worker loga regras
Get-Content C:\MFSAP\Worker\logs\worker-*.log -Tail 5
# Serviços marcados como Running
Get-Service MOTORFISCALSAPB1.*
```

### 2.7 Instalar o Addon nos clients SAP B1

Feito uma vez, via SAP B1 Server Manager:

```powershell
# No dev local — gerar o ZIP:
pwsh .\build\build-addon-lightweight.ps1
# Resultado: dist\MOTORFISCALSAPB1.Addon-1.0.0-x86.zip
```

No **SAP B1 Server Manager → Extension Manager**:

1. *Upload Extensions* → selecionar o ZIP gerado.
2. *Company Assignment* → atribuir à(s) empresa(s).
3. Marcar **"Install Mode = Mandatory"** e **"Start Mode = Automatic"**.
4. Em cada client, abrir o SAP B1: o addon é baixado e inicia sozinho
   (~30s).

Critério: no client, *Administração → Add-Ons → Add-Ons em execução*
mostra `MOTORFISCALSAPB1` com status `Conectado`.

---

## Parte 3 — Smoke test pós-deploy

Executar **após cada release** para validar a integração ponta-a-ponta.

### 3.1 API responde

```powershell
curl http://localhost:5080/health
# { "status":"Healthy", "hana":"Up", "serviceLayer":"Up" }
```

### 3.2 Worker está processando

```powershell
Get-Content C:\MFSAP\Worker\logs\worker-*.log -Tail 20 |
  Select-String "Rule cache refreshed"
# Deve aparecer a cada RuleCacheRefreshMinutes (default 5)
```

### 3.3 Resolver um TaxCode de ponta-a-ponta

```powershell
curl -X POST http://localhost:5080/api/fiscal/resolve `
  -H "Content-Type: application/json" `
  -d '{"cardCode":"C00001","bplId":1,"itemCode":"I00001","operationType":"SaleOut"}'
```

Critério: retorna JSON com `taxCode`, `cfop`, `cst`, `aliquota` — e a
mesma chamada executada 2x retorna o **mesmo** `taxCode` (idempotência
por assinatura fiscal — ver [`Signature.md`](Signature.md)).

### 3.4 Addon conecta no client

No SAP B1 Client:

1. Abrir uma Cotação / Pedido de Venda / NF de Entrada.
2. Preencher `CardCode`, `BPL_IDAssignedToInvoice`, item.
3. Menu *MOTORFISCAL → Resolver fiscal no documento*.
4. Observar o preenchimento automático de TaxCode / CFOP / CST.

Critério: linha fiscal preenchida **sem** o motor nativo SAP ter
determinado nada.

---

## Parte 4 — Rollback rápido

Se algo explodir depois de um deploy:

```powershell
# PowerShell como Administrador
sc.exe stop MOTORFISCALSAPB1.Api
sc.exe stop MOTORFISCALSAPB1.Worker

# Voltar para o release anterior (ex: tag v1.0.0):
cd D:\ProjetosGit\MOTORFISCALB1
git fetch --tags
git checkout v1.0.0

pwsh .\build\build-api.ps1    -Install
pwsh .\build\build-worker.ps1 -Install

sc.exe start MOTORFISCALSAPB1.Api
sc.exe start MOTORFISCALSAPB1.Worker
```

Para o **addon**: no Server Manager, *Extension Manager* →
*Rollback to previous version* (o SAP mantém as 2 últimas automaticamente).

---

## Parte 5 — Checklist imprimível

| # | Passo | OK |
|---|---|---|
| 1 | Windows Server 2019+ com .NET 8 Hosting Bundle | ☐ |
| 2 | Firewall porta 5080 liberada | ☐ |
| 3 | Conta de serviço `mfsap-svc` criada com `Log on as service` | ☐ |
| 4 | Repositório clonado em `D:\ProjetosGit\MOTORFISCALB1` | ☐ |
| 5 | `ASPNETCORE_ENVIRONMENT=Production` setado (Machine) | ☐ |
| 6 | `DOTNET_ENVIRONMENT=Production` setado (Machine) | ☐ |
| 7 | `build-api.ps1 -Install` executado com sucesso | ☐ |
| 8 | `build-worker.ps1 -Install` executado com sucesso | ☐ |
| 9 | `C:\MFSAP\Api\appsettings.Production.json` preenchido | ☐ |
| 10 | `C:\MFSAP\Worker\appsettings.Production.json` preenchido | ☐ |
| 11 | `sc.exe config` rodando com conta `mfsap-svc` | ☐ |
| 12 | Serviços `Running` após `sc.exe start` | ☐ |
| 13 | `/health` retorna `Healthy` | ☐ |
| 14 | Worker loga refresh do cache de regras | ☐ |
| 15 | Addon ZIP gerado e publicado no Extension Manager | ☐ |
| 16 | Addon conectado em pelo menos 1 client SAP B1 | ☐ |
| 17 | Smoke test `/api/fiscal/resolve` passou | ☐ |
| 18 | Smoke test addon no documento de marketing passou | ☐ |
