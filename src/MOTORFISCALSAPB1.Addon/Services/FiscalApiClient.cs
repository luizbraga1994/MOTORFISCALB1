using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MOTORFISCALSAPB1.Addon.Configuration;
using MOTORFISCALSAPB1.Shared.Contracts;
using Newtonsoft.Json;
using Serilog;

namespace MOTORFISCALSAPB1.Addon.Services
{
    /// <summary>
    /// Cliente HTTP enxuto para a API MOTORFISCALSAPB1.
    /// Não contém regra fiscal — apenas chama /api/fiscal/resolve.
    /// </summary>
    public sealed class FiscalApiClient
    {
        private readonly AddonSettings _settings;
        private readonly ILogger _log;

        public FiscalApiClient(AddonSettings settings, ILogger log)
        {
            _settings = settings;
            _log = log;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public async Task<FiscalResolutionResponse> ResolveAsync(
            FiscalResolutionRequest request,
            string correlationId,
            CancellationToken ct)
        {
            var url = _settings.ApiBaseUrl + "api/fiscal/resolve";
            var payload = JsonConvert.SerializeObject(request);
            var httpReq = (HttpWebRequest)WebRequest.Create(url);
            httpReq.Method = "POST";
            httpReq.ContentType = "application/json";
            httpReq.Accept = "application/json";
            httpReq.Headers["X-Correlation-Id"] = correlationId ?? Guid.NewGuid().ToString("N");
            httpReq.Timeout = _settings.HttpTimeoutSeconds * 1000;

            using (var reqStream = await httpReq.GetRequestStreamAsync().ConfigureAwait(false))
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                await reqStream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
            }

            try
            {
                using (var resp = (HttpWebResponse)await httpReq.GetResponseAsync().ConfigureAwait(false))
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    var body = await sr.ReadToEndAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<FiscalResolutionResponse>(body);
                }
            }
            catch (WebException wex)
            {
                var detail = "";
                if (wex.Response != null)
                {
                    using (var sr = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        detail = await sr.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
                _log.Warning(wex, "Falha chamando API: {Detail}", detail);
                throw new InvalidOperationException("Motor fiscal indisponível: " + wex.Message, wex);
            }
        }
    }
}
