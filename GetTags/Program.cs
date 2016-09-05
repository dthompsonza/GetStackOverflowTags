using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace GetTags
{
    class Program
    {
            

        static void Main(string[] args)
        {
            const string CONFIG_PATH = "GETTAG_CONFIG_PATH";

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.RollingFile("log-{Date}.txt")
                .CreateLogger();

            string configPath = Environment.GetEnvironmentVariable(CONFIG_PATH) ?? AppDomain.CurrentDomain.BaseDirectory;

            if (configPath.Length == 0)
            {
                Log.Warning("Configuration not found {ConfigurationKey}", CONFIG_PATH);
                return;
            }

            App app = new App(configPath);

            try
            {
                var result = app.Run();
                if (!result)
                    Log.Warning("There was a problem running the application, please check the logs");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "There was an unhandled exception while running the application");
            }

        }

    }
}
