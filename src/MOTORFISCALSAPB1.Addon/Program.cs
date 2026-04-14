using System;

namespace MOTORFISCALSAPB1.Addon
{
    internal static class Program
    {
        /// <summary>
        /// Argumento de linha de comando é a connection string do SAP B1 Client
        /// (fornecida automaticamente quando o addon é iniciado pelo Menu de Add-Ons).
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            var connection = args != null && args.Length > 0
                ? args[0]
                : Environment.GetEnvironmentVariable("SBO_ADDON_CONNECTION") ?? string.Empty;

            var bootstrapper = new AddonBootstrapper();
            return bootstrapper.Run(connection);
        }
    }
}
