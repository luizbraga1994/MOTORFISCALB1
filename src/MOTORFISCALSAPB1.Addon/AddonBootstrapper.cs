using System;
using System.IO;
using MOTORFISCALSAPB1.Addon.Configuration;
using MOTORFISCALSAPB1.Addon.Services;
using SAPbouiCOM;
using Serilog;

namespace MOTORFISCALSAPB1.Addon
{
    /// <summary>
    /// Ponto de entrada do addon. Estabelece conexão com o SAP B1 Client
    /// via SAPbouiCOM e registra o handler de eventos de documentos.
    /// </summary>
    public sealed class AddonBootstrapper
    {
        private Application _sapApp;
        private DocumentEventHandler _handler;

        public int Run(string connectionString)
        {
            var settings = new AddonSettings();
            var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(settings.LogPath, rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            try
            {
                _sapApp = ConnectToSap(connectionString);
                log.Information("Addon conectado ao SAP B1. CompanyDB={Db}", _sapApp.Company.CompanyDB);

                var apiClient = new FiscalApiClient(settings, log);
                _handler = new DocumentEventHandler(_sapApp, apiClient, settings, log);
                _handler.Attach();

                _sapApp.AppEvent += OnAppEvent;

                // Bloqueia enquanto o SAP estiver ativo.
                System.Windows.Forms.Application.Run();
                return 0;
            }
            catch (Exception ex)
            {
                log.Fatal(ex, "Addon falhou no startup.");
                return 1;
            }
        }

        private static Application ConnectToSap(string connectionString)
        {
            var bo = new SboGuiApi();
            bo.Connect(connectionString ?? Environment.GetCommandLineArgs()[0]);
            return bo.GetApplication();
        }

        private void OnAppEvent(BoAppEventTypes eventType)
        {
            if (eventType == BoAppEventTypes.aet_ShutDown
                || eventType == BoAppEventTypes.aet_CompanyChanged
                || eventType == BoAppEventTypes.aet_ServerTerminition)
            {
                _handler?.Detach();
                System.Windows.Forms.Application.Exit();
            }
        }
    }
}
