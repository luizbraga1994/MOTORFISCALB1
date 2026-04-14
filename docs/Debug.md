# Debug do addon no Visual Studio 2022

Este guia cobre o workflow de debug **antes** de empacotar como Extension.
Permite iterar (F5 + breakpoint) conectado ao SAP B1 Client real.

## Pré-requisitos

1. **SAP B1 Client 9.3+ instalado** (32-bit) na mesma máquina.
2. **Visual Studio 2022** com a carga `.NET desktop development`.
3. **Executar o VS2022 como Administrador** (clique direito → Run as
   Administrator). Requerido porque SAPbouiCOM é COM out-of-process.
4. Build em **x86** obrigatório. O `.csproj` já força `PlatformTarget=x86`.

## Passo 1 — Adicionar referências COM/Interop do SAP

Na primeira vez que clonar o repo, é preciso apontar para as DLLs locais
do SAP B1 Client (não redistribuíveis):

1. Solution Explorer → `MOTORFISCALSAPB1.Addon` → `Dependencies` → clique
   direito → **Add COM Reference** (ou **Add Project Reference →
   Browse**).
2. Adicionar:
   - `C:\Program Files (x86)\SAP\SAP Business One\SAPbouiCOM.dll`
   - `C:\Program Files (x86)\SAP\SAP Business One\SAPbobsCOM.dll`
3. Para **cada** referência adicionada, no painel Properties:
   - `Copy Local` = **False** (o SAP B1 Client fornece as DLLs em runtime;
     copiá-las quebra versão)
   - `Embed Interop Types` = **False**
   - `Isolated` = **False**

## Passo 2 — Configurar a Platform

1. Build → Configuration Manager.
2. Active solution platform = **x86** (ou crie se só houver `Any CPU`).
3. Garantir que `MOTORFISCALSAPB1.Addon` está marcado como Build para x86.

## Passo 3 — Abrir o SAP B1 Client

1. Login normal no SAP B1 Client na company de homologação.
2. Deixe a janela aberta. **Não inicie o addon** pelo menu Add-Ons (ele
   seria outro processo — queremos que o VS lance a nossa instância).

Se o addon já estiver registrado, configure-o como **Manual Start** em
`Administration → Add-Ons → Add-On Manager` para evitar conflito de
duas instâncias conectando na mesma sessão.

## Passo 4 — F5

O `launchSettings.json` já vem configurado com dois perfis:

- **MOTORFISCALSAPB1.Addon (SAP Dev)** — passa a dev connection string
  como `args[0]`.
- **MOTORFISCALSAPB1.Addon (Env Var)** — mesma string via variável
  `SBO_ADDON_CONNECTION` (fallback que `Program.Main` já aceita).

Selecione um perfil no dropdown ao lado do botão de Play e pressione
**F5**. O addon:

1. Inicia com a dev connection string `0,0,SAPBDatev,PLomView`
   (UTF-16 hex).
2. `SboGuiApi.Connect(...)` detecta que é uma sessão de desenvolvimento
   e **atacha à sessão do SAP B1 Client já aberto**.
3. `DocumentEventHandler.Attach()` passa a receber `ItemEvent` /
   `FormDataEvent` dos documentos de marketing.

Pontos sugeridos para breakpoint:

| Arquivo | Linha/método | Uso |
|---|---|---|
| `AddonBootstrapper.cs` | `Run()` início | Verificar connection string recebida |
| `DocumentEventHandler.cs` | `OnItemEvent()` início | Ver FormType / ItemUID do evento |
| `DocumentEventHandler.cs` | onde chama `_apiClient.ResolveAsync` | Inspecionar contexto enviado |
| `Services/FiscalApiClient.cs` | `ResolveAsync` | Ver request/response HTTP |
| `Services/Debouncer.cs` | `Debounce` | Checar cancel-previous |

## Passo 5 — Verificar

1. No SAP B1 abra **Pedido de Venda** (ou qualquer doc de marketing).
2. Digite um `CardCode` válido → breakpoint em `OnItemEvent` deve parar
   após ~400ms (debounce).
3. Adicione uma linha com um `ItemCode` → breakpoint em `ResolveAsync`
   deve parar com o contexto completo.
4. Confirme no SAP B1 que o TaxCode/CFOP/CST foram escritos na linha.

## Troubleshooting

**`SboGuiApi.Connect` lança `HRESULT 0x80004005`**
→ VS não está como Admin, ou build ≠ x86, ou SAP B1 Client está em outra
sessão Windows.

**`COMException: Class not registered`**
→ O SAP B1 Client não está instalado, ou está em versão 64-bit enquanto
o projeto é x86. Rebuilde em x64 e troque as referências para as DLLs
em `C:\Program Files\SAP\SAP Business One\`.

**Addon conecta mas `_sapApp.Company.CompanyDB` vem vazio**
→ A sessão do SAP B1 Client não fez login ainda. Logue antes do F5.

**`ItemEvent` não dispara**
→ Verifique se o FormType aberto está na lista do
`DocumentEventHandler._supportedFormTypes`. Se for um form customizado,
adicione o ID lá.

**Breakpoint fica "hollow" (não será atingido)**
→ `Debug → Options → Debugging → General`: desmarque "Enable Just My Code"
e "Require source files to exactly match the original version".

## Iteração rápida

Com o VS anexado, você pode:

- **Edit & Continue**: mudanças em métodos sem reiniciar o addon (limitado
  em net48).
- **Stop (Shift+F5)** e **F5** para reiniciar — o SAP B1 Client continua
  aberto e preserva a sessão.
- Log em tempo real em `src\MOTORFISCALSAPB1.Addon\bin\x86\Debug\logs\`
  (Serilog rolling file).

## Próximos passos

Validado o fluxo de debug, siga o empacotamento LightWeight em
[`Addon.md`](Addon.md) e o preenchimento do **Extension Registration
Data Generator** (o tool com os campos Extension Name / Version /
Namespace etc.).
