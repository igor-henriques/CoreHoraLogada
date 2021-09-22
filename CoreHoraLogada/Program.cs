using CoreHoraLogada.Watchers;
using CoreHoraLogadaDomain.Factory;
using CoreHoraLogadaDomain.Repository;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.License;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CoreHoraLogada
{
    public class Program
    {
        private readonly ILogger<Program> logger;
        private static LicenseControl license = new LicenseControl();
        private static ManualResetEvent quitEvent = new ManualResetEvent(false);
        private static IHost host;

        public Program(ILogger<Program> logger)
        {
            this.logger = logger;
        }

        static async Task Main()
        {
            try
            {
                CreateDefaultDirectories();

                host = CreateHostBuilder().Build();
                await host.Services.GetRequiredService<Program>().Run();                
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private static IHostBuilder CreateHostBuilder()
        {
            string connectionString = ConnectionBuilder.GetConnectionString();

            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {                    
                    services.AddTransient<Program>();

                    services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
                    ServiceLifetime.Transient,
                    ServiceLifetime.Transient);

                    services.AddTransient<IServerRepository, ServerRepository>();
                    services.AddTransient<ISaqueRepository, SaqueRepository>();
                    services.AddTransient<IRoleRepository, RoleRepository>();

                    services.AddSingleton<ItemAwardFactory>();
                    services.AddSingleton<ServerConnection>();
                    services.AddSingleton<Definitions>();
                    services.AddTransient<MessageFactory>();

                    services.AddTransient<ChatWatch>();

                    services.AddLogging(
                    builder =>
                    {
                        builder.AddFilter("Microsoft", LogLevel.Warning)
                               .AddFilter("System", LogLevel.Warning)
                               .AddFilter("NToastNotify", LogLevel.Warning)
                               .AddConsole();
                    });
                });
        }
        private async Task Run()
        {            
            await InitializeWatchers();

            await InitializeLicenseService();

            logger.LogInformation("TODOS OS MÓDULOS FORAM INICIALIZADOS\n\n");
            logger.LogInformation("PROGRAMADO POR IRONSIDE. REPORT BUG: Ironside#3862");
            Stop();
        }

        private static void CreateDefaultDirectories()
        {
            string configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configurations/");

            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);
        }

        private async Task InitializeWatchers()
        {
            logger.LogInformation("INICIALIZANDO WATCHER\n");
            host.Services.GetRequiredService<ChatWatch>();                        
        }

        private async Task InitializeLicenseService()
        {
            logger.LogInformation("INICIALIZANDO SISTEMA DE LICENÇA\n");
            CoreLicense licenseConfigs = JsonConvert.DeserializeObject<CoreLicense>(await File.ReadAllTextAsync("./Configurations/License.json"));
            await license.Start(licenseConfigs.User, licenseConfigs.Licensekey, licenseConfigs.Product);
        }

        private static void Stop()
        {
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                quitEvent.Set();
                eArgs.Cancel = true;
            };

            quitEvent.WaitOne();
        }
    }
}
