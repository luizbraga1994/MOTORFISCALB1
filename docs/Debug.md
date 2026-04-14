# Debug do addon no Visual Studio 2026 (ou 2022)

Este guia cobre o workflow de debug **antes** de empacotar como Extension.
Permite iterar (F5 + breakpoint) conectado ao SAP B1 Client real.

> Testado no **Visual Studio Community 2026**. No VS 2022 17.12+ o fluxo é
> idêntico; diferença apenas no nome das cargas de workload.

## Pré-requisitos

1. **SAP B1 Client** instalado na mesma máquina (32-bit **ou** 64-bit —
   o bitness do client determina com qual plataforma você vai debugar).
2. **.NET 10 SDK** — já vem embutido no VS 2026 (no VS 2022 17.12+
   também, mas confirme em `dotnet --list-sdks`).
3. **Visual Studio 2026 Community** (ou 2022 17.12+) com as cargas:
   - `.NET desktop development`
   - Opcional: `.NET Multi-platform App UI development` (para Hot
     Reload completo de WinForms).
4. **Executar o VS como Administrador** (clique direito → Run as
   Administrator). Requerido porque SAPbouiCOM é COM out-of-process.
5. Build na plataforma **que bate com o bitness do SAP B1 Client**:
   - Client 32-bit → `Platform = x86`.
   - Client 64-bit → `Platform = x64`.
   O `.csproj` expõe `<Platforms>x86;x64</Platforms>`; escolha no
   Configuration Manager.

## Passo 1 — Adicionar referências COM/Interop do SAP

Na primeira vez que clonar o repo, é preciso apontar para as DLLs locais
do SAP B1 Client (não redistribuíveis):

1. Solution Explorer → `MOTORFISCALSAPB1.Addon` → `Dependencies` → clique
   direito → **Add COM Reference** (ou **Add Project Reference →
   Browse**).
2. Adicionar, conforme o bitness do SAP B1 Client instalado:
   - **Client 32-bit** (`C:\Program Files (x86)\SAP\SAP Business One\`):
     `SAPbouiCOM.dll`, `SAPbobsCOM.dll`.
   - **Client 64-bit** (`C:\Program Files\SAP\SAP Business One\`):
     mesmas DLLs, caminho sem `(x86)`.
   Os Interop assemblies são AnyCPU — funcionam com `Platform=x86` **e**
   `x64`. O que muda é o bitness do processo em runtime, resolvendo o
   COM server correto.
3. Para **cada** referência adicionada, no painel Properties:
   - `Copy Local` = **False** (o SAP B1 Client fornece as DLLs em runtime;
     copiá-las quebra versão)
   - `Embed Interop Types` = **False**
   - `Isolated` = **False**

## Passo 2 — Configurar a Platform

1. Build → Configuration Manager.
2. Active solution platform = **x86** ou **x64** (bater com o bitness do
   SAP B1 Client instalado).
3. Garantir que `MOTORFISCALSAPB1.Addon` está marcado como Build **só**
   para a plataforma escolhida (nas outras, desmarque Build para evitar
   erros de referência COM apontando pro bitness oposto).

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
→ VS não está como Admin (no VS 2026, o shield na barra superior indica
modo Admin), ou o bitness do build não bate com o SAP B1 Client, ou o
Client está em outra sessão Windows.

**`COMException: Class not registered` (HRESULT `0x80040154`)**
→ **Mismatch de bitness**. Se o SAP B1 Client é 64-bit, use
`Platform=x64` e aponte referências para `C:\Program Files\SAP\SAP
Business One\`. Se é 32-bit, use `Platform=x86` e
`C:\Program Files (x86)\SAP\SAP Business One\`.

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

- **Edit & Continue**: mudanças em métodos sem reiniciar o addon (em
  .NET 10 funciona bem com Hot Reload no VS 2026/2022 para muitos casos).
- **Stop (Shift+F5)** e **F5** para reiniciar — o SAP B1 Client continua
  aberto e preserva a sessão.
- Log em tempo real em
  `src\MOTORFISCALSAPB1.Addon\bin\<x86|x64>\Debug\logs\`
  (Serilog rolling file).

## Próximos passos

Validado o fluxo de debug, siga o empacotamento LightWeight em
[`Addon.md`](Addon.md) e o preenchimento do **Extension Registration
Data Generator** (o tool com os campos Extension Name / Version /
Namespace etc.).
