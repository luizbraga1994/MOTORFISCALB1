# Assinatura fiscal e criação idempotente de TaxCode

## Por que uma assinatura?

Se a regra resolveu, por exemplo, para:

```
CFOP=5102, CST-ICMS=000, ALIQ-ICMS=18, CST-IPI=52, ALIQ-IPI=0,
CST-PIS=01, ALIQ-PIS=1.65, CST-COFINS=01, ALIQ-COFINS=7.6, TEM-ST=N
```

…então o TaxCode SAP que **representa** esse resultado deve ser exatamente o
mesmo da próxima vez que a resolução produzir esses mesmos valores — caso
contrário o SAP ficaria entulhado de TaxCodes redundantes. A **assinatura
fiscal** é o hash determinístico desse resultado, usado como chave do
mapeamento em `@MF_TAXMAP`.

## Forma canônica

`FiscalSignature.Canonical` é uma string com todos os campos relevantes
concatenados por `|`, em ordem fixa, com decimais normalizados para 4 casas
(`Invariant`), booleanos como `Y/N`, strings em upper-case e campos nulos
como string vazia. Exemplo (abreviado):

```
5102|00|52|01|01|18.0000|0.0000|1.6500|7.6000|N|0.0000|N|0.0000
```

## Hash

`SHA-256(Canonical)` → string hex de 64 caracteres.

## TaxCode

`TaxCodeCodeGenerator`: `"MF" + hash.Substring(0, 6).ToUpperInvariant()`.
Total: 8 caracteres, dentro do limite do SAP.

Colisão prática de SHA-256 truncado para 24 bits — considerando um universo
realista de ~10³ combinações por empresa, a probabilidade de colisão é
desprezível (< 10⁻⁴). Caso ocorra, a criação no SAP falha com "já existe" e
o TaxCodeEnsurer registra o conflito no log.

## Fluxo idempotente

```
TaxCodeEnsurer.EnsureAsync(result, ct)
  1. sighash = SHA-256(canonical)
  2. mapping = @MF_TAXMAP[sighash]
     -> se existe, retorna (taxCode, criado=false)
  3. lock = IDistributedFiscalLock.Acquire("TAXMAP:" + sighash)
     -> insert em @MF_LOCK com TTL + owner GUID
  4. double-check @MF_TAXMAP[sighash]
     -> se existe (alguém criou enquanto esperávamos), libera lock e retorna
  5. code = "MF" + hash[0..6]
     SapTaxCodeService.EnsureAsync(code, groups)
     -> POST SalesTaxCodes / PurchaseTaxCodes (tratamento de "já existe")
  6. @MF_TAXMAP INSERT (Code=sighash, U_TAXCODE=code, U_CANONICAL=canonical, ...)
  7. libera lock
  8. retorna (taxCode=code, criado=true)
```

## Lock distribuído

Implementação: `HanaDistributedFiscalLock` em `Infrastructure/Locking`.

- Chave: `Code` da linha em `@MF_LOCK` = `"TAXMAP:" + sighash`.
- TTL default: 30 s (configurável).
- Aquisição: `INSERT` — se violar PK, o lock está tomado; espera e tenta de
  novo com backoff (50 ms, 100 ms, 200 ms, …, até 2 s). Máx 50 tentativas.
- Expiração: antes de cada tentativa, tenta `DELETE WHERE Code=? AND
  U_EXPIRES_AT < now` (limpeza oportunística).
- Release: `DELETE WHERE Code=? AND U_OWNER=?` — só remove se ainda formos os
  donos.

## Verificação por teste

`TaxCodeEnsurerTests` inclui um teste que dispara **10 chamadas paralelas**
com o mesmo hash e um `SerialLock` in-memory simulando o `@MF_LOCK`. Assertiva
final: `_sapService.CreationCount == 1`.

## Persistência no SAP B1

Embora o motor não use a **determinação** fiscal nativa, o TaxCode criado via
Service Layer **é** materializado nas tabelas nativas de localização BR
(`OSTA` + `STA1`), incluindo os campos `U_TX_cclas`, `U_ICMSMod`, `U_ReduICMS`,
`U_Isento`, `U_AliqDest`, `U_PDif` etc. O `SapTaxCodeService` preenche esses
campos no payload do POST para `SalesTaxCodes` / `PurchaseTaxCodes`,
derivando os valores a partir do `FiscalResolutionResult` (alíquota, redução,
flags ST/DIFAL). Assim, o TaxCode gerado é fiscalmente **consumível** por
qualquer relatório/SPED baseado nas tabelas nativas.

Ver `docs/HanaQueries.md` para schemas de `OSTA`, `STA1`, `OWHT`.

## Garantias

- Idempotência: mesmas entradas → mesmo TaxCode, sempre.
- Atomicidade: exatamente uma criação no SAP por assinatura, mesmo sob
  concorrência de múltiplas instâncias (API + Worker + outras).
- Observabilidade: `@MF_LOG` registra `SigHash`, `TaxCode`, `Created` em todo
  `resolve`, permitindo auditar quantos TaxCodes foram criados por dia.
