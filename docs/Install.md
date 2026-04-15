# Instalação e validação de estrutura

Toda a estrutura SAP B1 usada pelo motor fiscal (UDTs, UDFs, UDOs) é criada
via **Service Layer**. Nenhum DDL direto em HANA é emitido para essa
finalidade. O manifesto autoritativo é `manifests/structure.json`.

## Fluxo

1. Operador chama `POST /api/structure/validate` (ou `/install`) apontando
   para o manifesto (ou usando o configurado em `Structure:ManifestPath`).
2. `ManifestValidator` checa tipos, duplicidades e consistência básica.
3. `SapStructureService`:
   - Autentica na Service Layer (`/b1s/v1/Login`).
   - Para cada UDT: consulta `UserTablesMD('<nome sem @>')`. Se 404 e modo
     `Execution`, `POST UserTablesMD`.
   - Para cada UDF: consulta CUFD (via `ISapReadPort`) com `TableID` e
     `AliasID` normalizados. Se não existir e modo `Execution`,
     `POST UserFieldsMD`.
   - Para cada UDO: `GET UserObjectsMD('CODE')`, se ausente `POST`.
4. Progresso reportado em tempo real via SignalR (`/hubs/structure`).
5. Retorna `StructureExecutionReport` contendo itens criados, ignorados
   (já existiam) e falhas — operação é **continuation on error** por item.

## Modos

- `Validation` (dry-run) — não cria nada, apenas relata o delta.
- `Execution` — cria o que estiver faltando. Idempotente: rodar duas vezes
  não duplica estruturas.

## Convenção crítica de naming

| Entidade | CUFD.TableID | SL POST TableName | SL POST Name | Coluna física HANA |
|---|---|---|---|---|
| UDF em tabela padrão (OBPL.U_MF_ATIVIDADE) | `OBPL` | `OBPL` | `MF_ATIVIDADE` | `U_MF_ATIVIDADE` |
| UDF em UDT (@MF_RULE.U_DESCRICAO) | `@MF_RULE` | `MF_RULE` | `DESCRICAO` | `U_DESCRICAO` |

O helper `SapNamingConventions` (em `MOTORFISCALSAPB1.Shared.Helpers`) é a
única fonte de verdade para essas transformações.

## Exemplo de chamada

```bash
curl -X POST http://localhost:5080/api/structure/install \
     -H "X-Correlation-Id: $(uuidgen)" \
     -H "Content-Type: application/json" \
     -d '{"manifestPath":"manifests/structure.json","mode":"Execution"}'
```

O hub SignalR emite mensagens:

```json
{ "phase": "UserTables",  "item": "MF_RULE",      "status": "Created" }
{ "phase": "UserFields",  "item": "OBPL.MF_ATIVIDADE", "status": "Exists" }
{ "phase": "UserObjects", "item": "MF_RULE",      "status": "Created" }
```
