using System;
using System.Configuration;

namespace MOTORFISCALSAPB1.Addon.Configuration
{
    /// <summary>
    /// Configurações do addon, lidas do App.config.
    /// </summary>
    public sealed class AddonSettings
    {
        public string ApiBaseUrl { get; }
        public int HttpTimeoutSeconds { get; }
        public int DebounceMilliseconds { get; }
        public string LogPath { get; }

        public AddonSettings()
        {
            ApiBaseUrl = ConfigurationManager.AppSettings["MF.ApiBaseUrl"] ?? "http://localhost:5080/";
            HttpTimeoutSeconds = ParseInt(ConfigurationManager.AppSettings["MF.HttpTimeoutSeconds"], 15);
            DebounceMilliseconds = ParseInt(ConfigurationManager.AppSettings["MF.DebounceMilliseconds"], 400);
            LogPath = ConfigurationManager.AppSettings["MF.LogPath"] ?? "logs\\addon-.log";

            if (!ApiBaseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                ApiBaseUrl += "/";
            }
        }

        private static int ParseInt(string? value, int fallback)
        {
            return int.TryParse(value, out var v) ? v : fallback;
        }
    }
}
