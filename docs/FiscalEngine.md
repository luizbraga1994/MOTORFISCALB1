# Motor fiscal — estratégia de regras

Objetivo: dado um `FiscalResolutionContext`, encontrar a regra aplicável mais
específica e retornar um `FiscalResolutionResult` determinístico.

## Contexto (entrada)

`FiscalResolutionContext` agrega, em um único objeto forte:

- Parceiro: `CardCode`, `CardType`, UF, município, CNPJ/CPF, contribuinte ICMS.
- Transação: `ConsumidorFinal` (vem do `IndFinal` do header do documento
  de marketing, não do BP).
- Filial: `BPLId`, UF, regime tributário (`OBPL.ProfFax`).
- Item: `ItemCode`, `NCM`, `CEST`, `Origem`.
- Operação: `TipoOperacao` (enum), `Quantidade`, `ValorUnitario`.
- Derivado: `IsOperacaoInterestadual()` (UF filial ≠ UF parceiro).

## Prioridade (funil de especificidade)

Quando há múltiplas regras candidatas, a `FiscalRule.Especificidade()` soma
pesos dos escopos declarados:

| Escopo | Peso |
|---|---|
| CardCode | 100 |
| ItemCode | 100 |
| BplId | 50 |
| NCM | 20 |
| TipoOperacao | 10 |
| UF origem | 5 |
| UF destino | 5 |
| Contribuinte ICMS | 3 |
| Consumidor final | 3 |
| Regime | 2 |

A ordem natural do resolvedor é:

1. Parceiro + item + filial
2. Item + operação + UF
3. NCM + operação + perfil de parceiro
4. Genérica
5. Fallback (se nenhuma regra vige) → `DomainException`

## Critério de desempate

```
ORDER BY Especificidade DESC, Prioridade DESC, Id ASC
```

Quando duas regras empatam em especificidade e prioridade, o `Id ASC` garante
determinismo. Qualquer empate verdadeiro (> 1 regra com mesma tupla) gera um
`LogWarning` com as regras concorrentes, mas o resultado continua
determinístico.

## DIFAL

`DifalCalculator` aplica o diferencial de alíquota (EC 87/2015 / LC 190/2022)
quando:

- `IsOperacaoInterestadual() == true`
- `Parceiro.ConsumidorFinal == true`
- Resultado da regra tem `TemIcms == true`

O partilhamento é 100% para o estado de destino, conforme LC 190/2022
(vigente desde 2022).

## Saída

`FiscalResolutionResult`:

```
TaxCodeSap, CFOP,
CstIcms, CstIpi, CstPis, CstCofins,
AliquotaIcms, AliquotaIpi, AliquotaPis, AliquotaCofins,
TemST, Mva, TemDifal, AliquotaDifal, ValorDifal,
RegraAplicadaId, AssinaturaFiscal, TaxCodeCriadoAgora
```

## Retenções (OWHT)

Para operações de entrada (compras, serviços tomados), o motor enriquece o
resultado com informações de retenção lidas em `OWHT` (IR, INSS, CSLL, PIS,
COFINS, CSRF, ISS retido). A seleção é feita por `WTCode` quando a regra
aponta retenção específica, ou por `Category + Section + EffecDate` para
cálculo automático. As colunas `U_TX_DescReceita` e `U_TX_Motiv` são
preservadas no resultado quando aplicável.

## Auditoria

Sempre registrada em `@MF_LOG` via `try/finally` no `FiscalEngine`. Mesmo em
exceção, o log contém `CorrelationId`, `Request`, `Error` e
`DurationMs` — útil para pós-mortem.
