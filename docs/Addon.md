# Addon SAP B1 — MOTORFISCALSAPB1 (LightWeight)

Projeto **.NET Framework 4.8 x86** (`MOTORFISCALSAPB1.Addon`) empacotado como
**addon LightWeight** — sem instalador MSI, sem InstallShield, sem cópia
manual por estação. Apenas um **ZIP auto-contido** registrado no SAP B1
Server, que o AddOn Launcher distribui automaticamente para cada cliente na
primeira execução.

## O que é LightWeight?

| | Addon clássico | **Addon LightWeight** |
|---|---|---|
| Empacotamento | `.ard` + `.exe` installer MSI | `.zip` com `.exe` + DLLs |
| Instalação por estação | Sim (cada cliente roda o MSI) | **Não** — SAP Client copia do server |
| `AddOnInstallerIni.ini` | Obrigatório | Não usado |
| Assinatura digital | Recomendada | Opcional |
| Atualização | Reinstalar em cada estação | Substituir o ZIP no server |
| Diretório runtime | `Program Files` | `%LOCALAPPDATA%\SAP\SAP Business One\AddOns\MOTORFISCALSAPB1` |

## Estrutura do pacote

Depois de rodar `build/build-addon-lightweight.ps1`, o ZIP contém:

```
MOTORFISCALSAPB1.Addon.exe          <- entry point (STAThread Main)
MOTORFISCALSAPB1.Addon.exe.config   <- App.config com MF.ApiBaseUrl etc.
MOTORFISCALSAPB1.Shared.dll
Newtonsoft.Json.dll
Serilog.dll
Serilog.Sinks.File.dll
addon.manifest.json                 <- metadata (name, version, platform)
```

**Não** inclui `SAPbouiCOM.dll` / `SAPbobsCOM.dll`: essas DLLs vêm com o
SAP B1 Client instalado na máquina e são resolvidas em runtime. O script de
build remove automaticamente se tiverem sido copiadas por engano.

## Build

No Windows, com .NET SDK e SAP B1 Client instalados:

```powershell
pwsh .\build\build-addon-lightweight.ps1 -Version 1.0.0
```

Artefato gerado em `dist/MOTORFISCALSAPB1.Addon-1.0.0-x86.zip`.

## Registro no SAP B1

1. Logue no SAP B1 como Superusuário.
2. `Administration → Add-Ons → Add-On Administration`.
3. Botão **Register Add-On**.
4. Em **Installer Package File**, aponte para o `.zip` gerado. O SAP B1
   reconhece automaticamente o pacote como LightWeight.
5. Marque **Force Install on Next Logon** se quiser distribuição automática.
6. Nível de execução: **Default / Automatic Start**.
7. `OK`. O servidor copia o ZIP para `%SharedPath%\AddOnsLocal\`; no próximo
   logon de cada cliente, o AddOn Launcher baixa, extrai em
   `%LOCALAPPDATA%\SAP\SAP Business One\AddOns\MOTORFISCALSAPB1\` e executa
   passando a connection string.

## Execução

1. SAP B1 Client chama `MOTORFISCALSAPB1.Addon.exe "<connectionString>"`.
2. `Program.Main` lê o argumento (ou `SBO_ADDON_CONNECTION` como fallback).
3. `AddonBootstrapper`:
   - `SboGuiApi.Connect(connectionString)`
   - registra `AppEvent` (shutdown/companyChanged/serverTermination)
   - ativa `DocumentEventHandler` (ItemEvent + FormDataEvent nos FormTypes
     de marketing listados abaixo)
   - bloqueia em `System.Windows.Forms.Application.Run()`
4. Ao receber `aet_ShutDown`, libera handlers e retorna 0.

## Documentos interceptados (FormType)

| FormType | Documento |
|---|---|
| `149` | Cotação de venda (OQUT) |
| `139` | Pedido de venda (ORDR) |
| `140` | Entrega (ODLN) |
| `141` | Devolução de venda (ORDN) |
| `142` | Nota de crédito (ORIN) |
| `133` | Nota fiscal de saída (OINV) |
| `18`  | Nota fiscal de entrada (OPCH) |
| `143` | Recebimento de mercadorias (OPDN) |
| `142000002` | Pedido de compra (OPOR) |
| `540`, `540000140` | Documentos compostos |

## Debounce e eventos

- `CardCode` (ItemUID `4`) → recalcula todas as linhas.
- `BPLId` (ItemUID prefixo `BPL*`) → idem.
- Linha da matriz (ItemUID `38`, coluna `ItemCode`) → recalcula aquela linha.

Cada chave `(DocEntry, linha)` tem seu próprio `Debouncer`
(`MF.DebounceMilliseconds`, default 400 ms). Digitações subsequentes
cancelam o cálculo anterior, economizando chamadas à API.

## Supressão de recursão

Antes de gravar TaxCode/CFOP/CST de volta na matriz:

```
_suppress = true
Matrix.FlushToDataSource()    // consolida input do usuario
Matrix.Freeze(true)
... write cells ...
Matrix.Freeze(false)
_suppress = false             // em finally
```

O handler inicial verifica `_suppress` e ignora o evento para evitar loop.

## Resiliência

- `FiscalApiClient` usa `HttpWebRequest` + Newtonsoft.Json, define TLS 1.2
  e `X-Correlation-Id` por chamada.
- Qualquer 5xx/timeout é logado em `logs/addon-*.log` (Serilog rolling file)
  e o addon **não bloqueia** o usuário — o documento continua editável.
- Se a API voltar, a próxima digitação resolverá normalmente.

## Configuração (App.config)

```xml
<appSettings>
  <add key="MF.ApiBaseUrl"          value="http://fiscal-api:5080/" />
  <add key="MF.HttpTimeoutSeconds"  value="15" />
  <add key="MF.DebounceMilliseconds" value="400" />
  <add key="MF.LogPath"             value="logs\addon-.log" />
</appSettings>
```

Para alterar configuração em produção: edite o `.config` dentro do ZIP,
incremente a versão em `addon.manifest.json`, re-upload via
**Add-On Administration**. Na próxima entrada de cada usuário, o SAP
detecta a nova versão e redistribui.

## Atualização em produção

1. Ajuste `App.config` / código / `-Version`.
2. `pwsh .\build\build-addon-lightweight.ps1 -Version 1.1.0`.
3. `Administration → Add-Ons → Add-On Administration → Update Version`.
4. `Administration → Add-Ons → Pending Add-Ons` para monitorar a
   distribuição aos clientes.

## Troubleshooting

- **Addon não sobe**: verifique `%LOCALAPPDATA%\SAP\SAP Business One\Log\`
  e o `logs/addon-*.log` local.
- **Travou o SAP B1 Client**: provavelmente recursão não suprimida — use
  os breakpoints em `DocumentEventHandler.OnItemEvent` e confirme que
  `_suppress` é setado antes de escrever na matriz.
- **Erro de arquitetura**: o pacote **é x86**. Se o SAP B1 Client estiver
  em 64-bit (casos raros), rebuilde com `/p:Platform=x64`.
