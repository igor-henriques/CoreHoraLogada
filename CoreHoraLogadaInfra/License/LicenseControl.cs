using CoreHoraLogadaInfra.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreHoraLogadaInfra.License
{
    public class LicenseControl : BackgroundService
    {
        private readonly CoreLicense _license;
        private readonly ILogger<LicenseControl> _logger;
        private readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        private readonly HttpClient httpClient;

        public LicenseControl(ILogger<LicenseControl> logger, CoreLicense license)
        {
            this._logger = logger;
            this._license = license;

            this.httpClient = HttpClientFactory.Create();
        }

        protected override async Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
        {
            _logger.LogInformation("MÓDULO DE LICENÇA INICIADO");

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            this._license.Hwid = await UserHWID(cts.Token);

            if (string.IsNullOrEmpty(this._license.Hwid))
            {
                LogWriter.Write("Não foi possível obter o registro de HWID.");
                Process.GetCurrentProcess().Kill();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Check(cts.Token);
                await Task.Delay(TimeSpan.FromMinutes(30));

                cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(10));
            }
        }
        private async Task Check(CancellationToken cancellationToken)
        {
            try
            {
                var response = (State)int.Parse(await httpClient.GetStringAsync($"http://license.ironside.dev/api/license/{_license.User}/{_license.Licensekey}/{_license.Hwid}/{Enum.GetName(typeof(Product), (int)_license.Product)}", cancellationToken));

                var logMessage = response switch
                {
                    State.Erro => "Houve um erro na requisição da licença.",
                    State.Esgotado => "Sua licença já está registrada em outra instância.",
                    State.Inexiste => "Sua licença não existe.",
                    State.Expirado => "Sua licença está fora da validade.",
                    State.Inativo => "Sua licença não está ativa.",
                    State.InvalidProduct => "Sua não pode ser utilizada neste produto.",
                    State.Welcome => "Licença validada com sucesso.",
                    State.Valido => "Licença validada com sucesso.",
                    _ => "Houve um erro na requisição da licença."
                };

                logMessage += response != (State.Valido | State.Welcome) ? " Entre em contato com a administração. Discord: Ironside#3862" : default;

                _logger.LogInformation(logMessage);

                if (response != (State.Valido | State.Welcome))
                    Process.GetCurrentProcess().Kill();
            }
            catch (Exception e)
            {
                LogWriter.Write(e.ToString());
                Process.GetCurrentProcess().Kill();
            }
        }

        private async Task<string> UserHWID(CancellationToken cancellationToken)
        {
            string ipAddress = await client.GetStringAsync("https://ipv4.icanhazip.com/", cancellationToken);

            return ipAddress?.Replace("\n", default)?.Replace(".", default);
        }
    }
}
