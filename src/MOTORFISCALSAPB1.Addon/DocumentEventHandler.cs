using System;
using System.Threading;
using System.Threading.Tasks;
using MOTORFISCALSAPB1.Addon.Configuration;
using MOTORFISCALSAPB1.Addon.Services;
using MOTORFISCALSAPB1.Shared.Contracts;
using MOTORFISCALSAPB1.Shared.Enums;
using SAPbouiCOM;
using SapApp = SAPbouiCOM.Application;
using Serilog;

namespace MOTORFISCALSAPB1.Addon
{
    /// <summary>
    /// Ouve alterações relevantes em documentos de marketing (CardCode, BPLId e
    /// ItemCode da linha), monta o contexto mínimo e aplica o resultado
    /// retornado pela API na respectiva linha. Sem lógica fiscal.
    /// Cobre Cotações, Pedidos de Venda/Compra, Notas de Entrada/Saída,
    /// Entregas, Devoluções etc.
    /// </summary>
    public sealed class DocumentEventHandler
    {
        private readonly SapApp _app;
        private readonly FiscalApiClient _api;
        private readonly Debouncer _debouncer;
        private readonly ILogger _log;
        private bool _suppressReentrancy;

        // FormTypes que aceitamos escutar (documentos de marketing).
        private static readonly string[] SupportedFormTypes = new[]
        {
            "133", "139", "140", "141", "142", "149",
            "18", "143", "142000002", "540", "540000140"
        };

        public DocumentEventHandler(SapApp app, FiscalApiClient api, AddonSettings settings, ILogger log)
        {
            _app = app;
            _api = api;
            _log = log;
            _debouncer = new Debouncer(settings.DebounceMilliseconds);
        }

        public void Attach()
        {
            _app.ItemEvent += OnItemEvent;
        }

        public void Detach()
        {
            _app.ItemEvent -= OnItemEvent;
        }

        private void OnItemEvent(string formUid, ref ItemEvent pVal, out bool bubbleEvent)
        {
            bubbleEvent = true;
            if (_suppressReentrancy) return;
            if (pVal.BeforeAction) return;

            if (Array.IndexOf(SupportedFormTypes, pVal.FormTypeEx) < 0) return;

            try
            {
                // Evento após edição e "Item Pressed" de LinkedButton também passam aqui.
                if (pVal.EventType == BoEventTypes.et_VALIDATE || pVal.EventType == BoEventTypes.et_LOST_FOCUS)
                {
                    // Campos relevantes: CardCode (item 4), BPLID (item U_BPLID/U_BPLName, hidden in many forms),
                    // Item da linha (ItemCode na matriz 38).
                    if (pVal.ItemUID == "4" || pVal.ItemUID == "38" || pVal.ItemUID.Contains("BPL"))
                    {
                        ScheduleResolve(formUid, pVal);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Erro tratando evento do form {Form}", formUid);
            }
        }

        private void ScheduleResolve(string formUid, ItemEvent pVal)
        {
            var rowIndex = pVal.Row; // 1-based
            var key = formUid + "|" + rowIndex;

            _debouncer.Schedule(key, async ct =>
            {
                await ResolveAndApplyAsync(formUid, rowIndex, ct).ConfigureAwait(false);
            });
        }

        private async Task ResolveAndApplyAsync(string formUid, int rowIndex, CancellationToken ct)
        {
            try
            {
                var form = _app.Forms.Item(formUid);
                var tipoOp = MarketingDocTypeMap.Resolve(form.TypeEx);
                var correlation = Guid.NewGuid().ToString("N");

                var cardCode = TryGetEditValue(form, "4");
                var bplText = TryGetEditValue(form, "U_BPLID") ?? TryGetEditValue(form, "59"); // id visual padrão
                int bplId;
                int.TryParse(bplText, out bplId);

                var matrix = (Matrix)form.Items.Item("38").Specific;
                var itemCode = TryGetMatrixValue(matrix, "1", rowIndex);

                if (string.IsNullOrEmpty(cardCode) || bplId <= 0 || string.IsNullOrEmpty(itemCode))
                {
                    return;
                }

                // IndFinal e campo nativo do header do documento (Y/N) na localizacao BR.
                // Item UID pode variar por versao; tentamos os nomes mais comuns.
                var indFinal = TryGetEditValue(form, "IndFinal")
                               ?? TryGetEditValue(form, "U_IndFinal")
                               ?? "N";
                var consFinal = string.Equals(indFinal, "Y", StringComparison.OrdinalIgnoreCase);

                var req = new FiscalResolutionRequest
                {
                    CardCode = cardCode,
                    BplId = bplId,
                    ItemCode = itemCode,
                    TipoOperacao = Enum.TryParse<TipoOperacao>(tipoOp, out var t) ? t : TipoOperacao.VendaInterna,
                    ConsumidorFinal = consFinal,
                    CorrelationId = correlation
                };

                _log.Information("Chamando motor fiscal: BP={BP} BPL={BPL} Item={Item} Op={Op}",
                    req.CardCode, req.BplId, req.ItemCode, req.TipoOperacao);

                var resp = await _api.ResolveAsync(req, correlation, ct).ConfigureAwait(false);
                ApplyResult(form, matrix, rowIndex, resp);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Resolução fiscal falhou no form {Form} linha {Row}.", formUid, rowIndex);
                // Indisponibilidade da API não deve travar o SAP.
            }
        }

        private void ApplyResult(IForm form, Matrix matrix, int rowIndex, FiscalResolutionResponse resp)
        {
            _suppressReentrancy = true;
            try
            {
                form.Freeze(true);
                SetMatrixValue(matrix, "TaxCode", rowIndex, resp.TaxCode);
                SetMatrixValue(matrix, "CFOP",    rowIndex, resp.Cfop);
                SetMatrixValue(matrix, "CST",     rowIndex, resp.CstIcms);
            }
            finally
            {
                form.Freeze(false);
                _suppressReentrancy = false;
            }
        }

        private static string TryGetEditValue(IForm form, string itemUid)
        {
            try
            {
                var item = form.Items.Item(itemUid);
                var edit = item.Specific as EditText;
                return edit != null ? edit.Value : null;
            }
            catch { return null; }
        }

        private static string TryGetMatrixValue(Matrix matrix, string colUid, int rowIndex)
        {
            try
            {
                var cell = (EditText)matrix.Columns.Item(colUid).Cells.Item(rowIndex).Specific;
                return cell.Value;
            }
            catch { return null; }
        }

        private static void SetMatrixValue(Matrix matrix, string colUid, int rowIndex, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return;
                var cell = (EditText)matrix.Columns.Item(colUid).Cells.Item(rowIndex).Specific;
                cell.Value = value;
            }
            catch
            {
                // coluna pode não existir em todos os documentos
            }
        }
    }
}
