using CoreHoraLogadaDomain.Factory;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreHoraLogadaDomain.Repository
{
    public class ServiceProvider
    {
        public static IHostBuilder CreateHostBuilder()
        {
            string connectionString = ConnectionBuilder.GetConnectionString();

            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddTransient<ApplicationDbContext>();                    

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
    }
}
