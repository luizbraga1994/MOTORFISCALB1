# Arquitetura — MOTORFISCALSAPB1

Motor fiscal inteligente integrado ao SAP Business One. Resolve automaticamente
TaxCode SAP, CFOP, CST (ICMS/IPI/PIS/COFINS), alíquotas, ST e DIFAL para
**qualquer documento de marketing** (Cotação, Pedido de Venda, Entrega,
Nota Fiscal de Saída, Devolução, Nota de Crédito, Pedido de Compra, Recebimento
de Mercadorias, Nota de Entrada etc.), a partir de regras configuráveis, em
tempo real, sem usar o motor fiscal nativo do SAP B1.

## Princípios

1. **Clean Architecture + DDD** — 9 projetos, dependências apontando para o
   centro (Domain).
2. **Service Layer para criar estrutura** — UDT/UDF/UDO criados
   exclusivamente via `UserTablesMD`, `UserFieldsMD`, `UserObjectsMD`. **Proibido**
   DDL direto no HANA para essa finalidade.
3. **HANA só para leitura e tabelas próprias justificadas** — consultas a
   `OCRD`, `CRD1`, `CRD7`, `OBPL`, `OITM`, `CUFD` e escrita em UDTs próprios
   (`@MF_RULE`, `@MF_TAXMAP`, `@MF_LOG`, `@MF_LOCK`, `@MF_CFG`).
4. **Determinismo e idempotência** — a assinatura fiscal (SHA-256 sobre o
   resultado) garante que o mesmo resultado → mesmo TaxCode, sempre.
5. **Thread-safety** — criação de TaxCode protegida por lock distribuído em
   `@MF_LOCK` + double-check.
6. **Agnóstico ao documento** — o motor opera sobre a tupla
   `(CardCode, BPLId, ItemCode, TipoOperacao)`, presente em todos os
   documentos de marketing.

## Projetos

| Projeto | Responsabilidade |
|---|---|
| `MOTORFISCALSAPB1.Domain` | Entidades, agregados, VOs, regras de domínio |
| `MOTORFISCALSAPB1.Shared` | Contratos, enums, helpers de naming/hash, Result |
| `MOTORFISCALSAPB1.Application` | Casos de uso, orquestradores, portas |
| `MOTORFISCALSAPB1.Infrastructure` | HANA (Dapper), cache, lock, repositórios |
| `MOTORFISCALSAPB1.Integration.SapB1` | Service Layer client, MD services, TaxCodes |
| `MOTORFISCALSAPB1.Api` | HTTP API (Minimal APIs), SignalR, Swagger |
| `MOTORFISCALSAPB1.Worker` | BackgroundService de refresh de cache |
| `MOTORFISCALSAPB1.Addon` | .NET Framework 4.8 x86, UI API/DI API, debounce |
| `MOTORFISCALSAPB1.Tests` | xUnit, FluentAssertions, Moq |

## Fluxo de resolução fiscal

```
Addon / API
   │
   ▼
ResolveFiscalUseCase
   │
   ├─► FiscalContextBuilder  (lê BP, filial, item no HANA em paralelo)
   │
   ├─► FiscalEngine
   │      ├─► FiscalRuleResolver  (regras em cache, ordenadas por
   │      │      Especificidade DESC, Prioridade DESC, Id ASC)
   │      ├─► DifalCalculator     (interestadual + consumidor final)
   │      ├─► FiscalSignatureService
   │      └─► TaxCodeEnsurer      (idempotente, com lock distribuído)
   │
   └─► FiscalAuditRepository (try/finally — sempre grava)
```

## Fronteiras

- **Domain** não conhece HANA, Service Layer, HTTP ou UI API.
- **Application** depende apenas de portas (interfaces) definidas nela mesma.
- **Infrastructure** implementa portas de leitura HANA e repositórios UDT.
- **Integration.SapB1** implementa portas de Service Layer e criação de TaxCode.
- **Addon** não contém lógica fiscal — apenas coleta contexto, chama a API,
  aplica o retorno no documento.
