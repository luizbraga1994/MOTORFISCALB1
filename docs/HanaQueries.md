# Consultas HANA — MOTORFISCALSAPB1

O acesso ao HANA é **somente leitura** para tabelas nativas do SAP B1 e
leitura/escrita para as UDTs próprias do motor. Nenhum DDL (`CREATE TABLE`,
`ALTER TABLE`) é executado em HANA — a criação de estruturas usa Service
Layer.

## Convenções

- Schema da company definido por `Hana:Schema` (ex.: `SBO_COMP`). Sempre
  referenciado com aspas: `"SBO_COMP"."@MF_RULE"`.
- UDTs: nome físico começa com `@` no HANA (`"@MF_RULE"`) mas sem `@` em
  Service Layer.
- UDFs em UDTs próprias (ex.: `"@MF_RULE"."U_DESCRICAO"`) seguem o padrão
  SAP: coluna física com prefixo `U_`, Alias em Service Layer **sem** `U_`.
- **Preferir campos nativos da localização BR** em vez de UDF em tabela
  padrão — a estrutura atual não cria UDFs em `OCRD`, `OITM` ou `OBPL`.
  Tudo o que o motor precisa vem de: `OITM.ProductSrc`, `OITM.NCMCode`
  (→`ONCM`), `OITM.CESTCode` (→`OCEST`), `OBPL.ProfFax`, e do campo
  `IndFinal` no header dos documentos de marketing.
- Todas as leituras parametrizadas via Dapper (prevenção de SQL injection).

## Parceiro de negócios (OCRD + CRD1 + CRD7)

```sql
SELECT
    c."CardCode",
    c."CardName",
    c."CardType",
    c."LicTradNum",
    a."Address",
    a."AdresType",
    a."State",
    a."City",
    a."ZipCode",
    a."Country",
    t."TaxId1"
FROM "SBO_COMP"."OCRD" c
LEFT JOIN "SBO_COMP"."CRD1" a
       ON a."CardCode" = c."CardCode"
      AND a."AdresType" = 'S'
LEFT JOIN "SBO_COMP"."CRD7" t
       ON t."CardCode" = c."CardCode"
      AND t."Address"  = a."Address"
      AND t."AddrType" = a."AdresType"
WHERE c."CardCode" = :cardCode
```

## Filial (OBPL)

`ProfFax` é o campo nativo da localização BR para regime tributário.

```sql
SELECT
    "BPLId", "BPLName", "TaxIdNum", "State",
    "ProfFax"
FROM "SBO_COMP"."OBPL"
WHERE "BPLId" = :bplId
```

## Item (OITM + ONCM + OCEST)

Todos os atributos fiscais relevantes do item são campos **nativos** da
localização BR:

- `OITM.ProductSrc` — origem da mercadoria (0-8).
- `OITM.NCMCode` — FK para `ONCM.AbsEntry`; `ONCM.Code` é o NCM textual.
  Demais campos do NCM úteis ao motor: `U_TX_FatorTrib`, `U_TX_UTrib`,
  `U_TX_GerarRastreabilidade`, `U_TX_CodigoCest`.
- `OITM.CESTCode` — FK para `OCEST.AbsId`; `OCEST.Code` é o CEST textual
  (override por item; o CEST padrão do NCM fica em `ONCM.U_TX_CodigoCest`).

```sql
SELECT
    i."ItemCode", i."ItemName",
    n."Code"   AS "Ncm",
    c."Code"   AS "Cest",
    i."ProductSrc"
FROM "SBO_COMP"."OITM"  i
LEFT JOIN "SBO_COMP"."ONCM"  n ON n."AbsEntry" = i."NCMCode"
LEFT JOIN "SBO_COMP"."OCEST" c ON c."AbsId"    = i."CESTCode"
WHERE i."ItemCode" = :itemCode
```

## Existência de UDT/UDF (CUFD)

Regra de normalização:

- Para UDT: `TableID = '@MF_RULE'`, `AliasID = 'DESCRICAO'` (sem `U_`).
- A estrutura atual não cria UDFs em tabelas padrão; o padrão continua
  valendo caso seja necessário no futuro: `TableID = 'OCRD'`, `AliasID`
  sem o prefixo `U_`.

```sql
SELECT COUNT(1)
FROM "SBO_COMP"."CUFD"
WHERE "TableID" = :tableId AND "AliasID" = :aliasId
```

## Regra fiscal (UPSERT em @MF_RULE)

```sql
MERGE INTO "SBO_COMP"."@MF_RULE" t
USING (SELECT :code AS "Code" FROM "DUMMY") s
   ON t."Code" = s."Code"
WHEN MATCHED THEN UPDATE SET
    "Name"           = :name,
    "U_DESCRICAO"    = :descricao,
    "U_PRIORITY"     = :priority,
    "U_ACTIVE"       = :active,
    "U_CARDCODE"     = :cardCode,
    "U_ITEMCODE"     = :itemCode,
    "U_BPLID"        = :bplId,
    "U_NCM"          = :ncm,
    "U_TIPO_OP"      = :tipoOp,
    "U_UF_ORIGEM"    = :ufOrigem,
    "U_UF_DESTINO"   = :ufDestino,
    "U_CONTRIB_ICMS" = :contribIcms,
    "U_CONSUMIDOR_FINAL" = :consumidorFinal,
    "U_REGIME"       = :regime,
    "U_VIG_INICIO"   = :vigInicio,
    "U_VIG_FIM"      = :vigFim,
    "U_RESULT_JSON"  = :resultJson
WHEN NOT MATCHED THEN INSERT (...columns...) VALUES (...values...);
```

## Mapeamento assinatura → TaxCode

```sql
SELECT "U_TAXCODE" FROM "SBO_COMP"."@MF_TAXMAP" WHERE "Code" = :sighash
```

## Lock distribuído (@MF_LOCK)

```sql
-- Tentativa de inserção do lock (falha se já existir e não expirou):
INSERT INTO "SBO_COMP"."@MF_LOCK"
  ("Code", "Name", "U_OWNER", "U_EXPIRES_AT", "U_CREATED_AT")
VALUES
  (:code, :code, :owner, :expiresAt, :createdAt);

-- Limpeza oportunística de locks expirados:
DELETE FROM "SBO_COMP"."@MF_LOCK"
WHERE "Code" = :code AND "U_EXPIRES_AT" < :now;
```

## Tabelas fiscais nativas do SAP B1 (somente leitura)

Os TaxCodes que criamos via Service Layer são materializados nas tabelas
nativas de localização brasileira do SAP B1. Os schemas relevantes estão
documentados em `Tabelas Fiscal.xlsx` (branch `main`). O motor **não** escreve
diretamente nelas — usa `SalesTaxCodes` / `PurchaseTaxCodes` do Service Layer.
Leituras são permitidas para diagnóstico e conferência.

### OSTT — Tipos de imposto

```sql
SELECT "AbsId", "Code", "Name", "IsVat", "TpsId", "NfTaxId", "PLABalance",
       "CreditCtrl", "Locked"
FROM "SBO_COMP"."OSTT"
```

### OSTA — Definições de TaxCode (cabeçalho)

Tabela onde cada TaxCode (ex.: `MF1A2B3C`) é persistido. Contém campos
nativos (Rate, Type, MinAmount, MaxAmount, VatExempt, TaxInPrice, Exempt,
TaxOnRI) e **campos customizados da localização BR**:

| Coluna | Significado |
|---|---|
| `U_TX_cclas` | Classificação fiscal |
| `U_ICMSMod` | Modalidade de base de cálculo ICMS |
| `U_ICMSTMod` | Modalidade base cálculo ICMS-ST |
| `U_ReduICMS` | Redução de base ICMS (%) |
| `U_Reducao1`, `U_Reducao2` | Reduções adicionais |
| `U_PercCrSN` | % crédito Simples Nacional |
| `U_PrecoFix`, `U_PrecoMin` | Preço fixo / mínimo (pauta) |
| `U_Lucro`, `U_Minimo`, `U_FatorPrc` | Margem, mínimo, fator de preço |
| `U_Isento` | Flag de isenção |
| `U_Base`, `U_AliqDest`, `U_PDif` | Base, alíquota destino, percentual DIFAL |
| `U_Medida`, `U_Unidades`, `U_Moeda` | Unidade de medida, unidades, moeda |
| `U_IntPart`, `U_Outros` | Participação interna, outros |

Consulta típica:

```sql
SELECT "Code", "Name", "Type", "Rate", "U_TX_cclas", "U_ICMSMod",
       "U_ReduICMS", "U_Isento", "U_PercCrSN", "U_AliqDest", "U_PDif"
FROM "SBO_COMP"."OSTA"
WHERE "Code" = :taxCode
```

### STA1 — Linhas do TaxCode (por UF/vigência)

Um mesmo `StaCode` pode ter várias linhas, cada uma com `Rate` próprio por
UF e data de vigência (`EfctDate`). Usada pelo SAP para tabela por UF.

```sql
SELECT "StaCode", "SttType", "EfctDate", "Rate",
       "U_ICMSMod", "U_ICMSTMod", "U_Base", "U_AliqDest", "U_PDif",
       "U_Reducao1", "U_Reducao2", "U_Isento"
FROM "SBO_COMP"."STA1"
WHERE "StaCode" = :taxCode
ORDER BY "EfctDate" DESC
```

### ONFT — Códigos NFT

```sql
SELECT "AbsId", "Code", "GPCId", "CESTrel", "Locked"
FROM "SBO_COMP"."ONFT"
```

### OWHT — Retenções (IR, INSS, CSLL, PIS, COFINS, CSRF, ISS retido)

Relevante para documentos de entrada. O motor consulta para enriquecer o
resultado fiscal em operações com retenção.

```sql
SELECT "WTCode", "WTName", "Type", "Category", "Section",
       "Rate", "Threshold", "MinTaxAmt", "RoundType", "BaseType",
       "U_TX_DescReceita", "U_TX_Motiv",
       "OutCSTCode", "InCSTCode",
       "EffecDate", "Inactive"
FROM "SBO_COMP"."OWHT"
WHERE "Inactive" = 'N'
```

### OTPS / TPS1 / TPS2 — Parâmetros de imposto

Metadados das combinações possíveis de parâmetros fiscais. Somente
referência:

```sql
SELECT "AbsId", "Code", "Descr" FROM "SBO_COMP"."OTPS";
SELECT "TpsId", "TpaId", "DispOrder", "Mandatory" FROM "SBO_COMP"."TPS1";
SELECT "TpsId", "TprId", "DispOrder" FROM "SBO_COMP"."TPS2";
```
