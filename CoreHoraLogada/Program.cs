using CoreHoraLogada.Infrastructure.Interfaces;
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
using System;

string connectionString = ConnectionBuilder.GetConnectionString();

try
{
    await Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
          services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

          services.AddSingleton<IServerRepository, ServerRepository>();
          services.AddSingleton<ISaqueRepository, SaqueRepository>();
          services.AddSingleton<IRoleRepository, RoleRepository>();

          services.AddSingleton<CoreLicense>();
          services.AddSingleton<ItemAwardFactory>();
          services.AddSingleton<ServerConnection>();
          services.AddSingleton<Definitions>();
          services.AddSingleton<MessageFactory>();

          services.AddHostedService<LicenseControl>();
          services.AddHostedService<ChatWatch>();

          services.AddLogging(builder =>
          {
              builder.AddFilter("Microsoft", LogLevel.Warning)
                      .AddFilter("System", LogLevel.Warning)
                      .AddFilter("NToastNotify", LogLevel.Warning)
                      .AddConsole();
          });
      }).Build().RunAsync();
}
catch (Exception e)
{
    LogWriter.Write($"Sistema finalizado inesperadamente\n\n{e.ToString()}");
    Environment.Exit(1);
}
