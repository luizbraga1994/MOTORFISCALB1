# MOTORFISCALSAPB1 — Documentação técnica

Motor fiscal inteligente integrado ao SAP Business One (Service Layer +
UI API + DI API + HANA). Resolve em tempo real TaxCode, CFOP, CST, alíquotas,
ST e DIFAL para qualquer documento de marketing, a partir de regras
configuráveis.

## Índice

- [Architecture.md](Architecture.md) — camadas, projetos e princípios.
- [Install.md](Install.md) — instalação de estrutura SAP (UDT/UDF/UDO) via
  Service Layer a partir de `manifests/structure.json`.
- [HanaQueries.md](HanaQueries.md) — consultas HANA usadas pelo motor.
- [FiscalEngine.md](FiscalEngine.md) — estratégia de regras e desempate.
- [Signature.md](Signature.md) — assinatura fiscal, hash e criação
  idempotente de TaxCode.
- [Addon.md](Addon.md) — comportamento do addon SAP B1 (UI API) e empacotamento LightWeight.
- [Debug.md](Debug.md) — workflow de debug no Visual Studio 2022 com SAP B1 Client.
- [Deployment.md](Deployment.md) — API e Worker como Windows Services.

## Regras inegociáveis

1. **UDT/UDF/UDO criados APENAS via Service Layer** — nunca DDL em HANA.
2. **HANA é read-only** para tabelas nativas SAP + read/write somente para as
   UDTs próprias `@MF_RULE`, `@MF_TAXMAP`, `@MF_LOG`, `@MF_LOCK`, `@MF_CFG`.
3. **Idempotência** garantida por assinatura fiscal SHA-256 → TaxCode.
4. **Sem motor fiscal nativo** — a determinação é 100% pelo motor próprio.
5. **Agnóstico ao documento** — cotação, pedido, NF de entrada/saída,
   devolução, crédito, GRPO, etc. Todos compartilham a tupla
   `(CardCode, BPLId, ItemCode, TipoOperacao)`.
