# Addon SAP B1 — comportamento

Projeto **.NET Framework 4.8 x86** (`MOTORFISCALSAPB1.Addon`). Usa UI API
(`SAPbouiCOM`) e, quando necessário, DI API (`SAPbobsCOM`). **Não contém
lógica fiscal**: somente coleta contexto do documento aberto, chama a API do
motor e aplica o retorno na matriz.

## Documentos suportados

O motor é agnóstico ao tipo de documento. O addon intercepta eventos nos
seguintes `FormType`s (todos os documentos de marketing, entrada e saída):

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

## Eventos e debounce

- `CardCode` (ItemUID `4`) — recalcula todas as linhas.
- `BPLId` (ItemUID com sufixo `BPL*`) — idem.
- Linha da matriz (ItemUID `38`, coluna `ItemCode`) — recalcula aquela linha.

Cada chave (`docEntry + linha`) tem seu próprio `Debouncer`
(`MF.DebounceMilliseconds`, default 400 ms). Uma nova digitação cancela o
cálculo anterior. Isso evita saturar a API enquanto o usuário ainda está
digitando.

## Supressão de recursão

Antes de escrever a coluna de TaxCode / CFOP / CST de volta na matriz, o
handler ativa um flag `_suppress = true`, chama `Matrix.FlushToDataSource()`
para consolidar o input do usuário, aplica `Freeze(true)`, atualiza as
células, e libera com `Freeze(false)` e `_suppress = false` no `finally`.

## Resiliência

- `FiscalApiClient` usa `HttpWebRequest` + Newtonsoft.Json, define TLS 1.2 e
  `X-Correlation-Id` por chamada.
- Qualquer erro 5xx / timeout é tratado como "API indisponível": o addon
  registra no Serilog (`logs/addon-*.log`) e **não bloqueia** o usuário —
  o documento continua editável.
- Configuração em `App.config`:

```xml
<appSettings>
  <add key="MF.ApiBaseUrl"          value="http://localhost:5080/" />
  <add key="MF.HttpTimeoutSeconds"  value="15" />
  <add key="MF.DebounceMilliseconds" value="400" />
  <add key="MF.LogPath"             value="logs\addon-.log" />
</appSettings>
```

## Ciclo de vida

```
SBO Menu Add-Ons -> Program.Main(connectionString)
         │
         ▼
AddonBootstrapper.Run()
  ├── SboGuiApi.Connect(connectionString)
  ├── registra AppEvent -> handler de shutdown
  ├── registra FormEvent / ItemEvent / MenuEvent
  └── System.Windows.Forms.Application.Run()  (blocking)
```

Quando o SAP envia `AppEvent = aet_ShutDown`, o bootstrapper dá `Dispose` nos
recursos e retorna 0.
