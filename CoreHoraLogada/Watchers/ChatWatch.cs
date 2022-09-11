using CoreHoraLogada.Infrastructure.Interfaces;
using CoreHoraLogadaDomain;
using CoreHoraLogadaDomain.Factory;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using CoreRankingInfra.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PWToolKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreHoraLogada.Watchers;

internal class ChatWatch : BackgroundService
{
    private readonly Random randomizer = new Random();
    private readonly ILogger<ChatWatch> logger;
    private readonly IServerRepository _serverContext;
    private readonly IRoleRepository _roleContext;
    private readonly ISaqueRepository _saqueContext;
    private readonly Definitions _definitions;
    private readonly MessageFactory _mFactory;
    private readonly string path;
    private List<CodeVerification> PlayerCodeVerificator;
    private long lastSize;
    private DateTime lastTopRank;

    public ChatWatch(
        ILogger<ChatWatch> logger,
        IServerRepository serverContext,
        IRoleRepository roleContext,
        ISaqueRepository saqueContext,
        Definitions definitions,
        MessageFactory mFactory)
    {
        this.lastTopRank = new DateTime(1990, 1, 1);
        this.logger = logger;
        _serverContext = serverContext;
        _roleContext = roleContext;
        _saqueContext = saqueContext;
        _definitions = definitions;
        _mFactory = mFactory;

        this.path = $"{Path.Combine(_serverContext.GetLogsPath(), "world2.chat")}";
        lastSize = GetFileSize(path);
        PWGlobal.UsedPwVersion = _serverContext.GetPwVersion();
        PlayerCodeVerificator = new List<CodeVerification>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MÓDULO DE CHAT INICIALIZADO");

        Timer hourlyTimer = new Timer(async _ =>
        {
            PlayerCodeVerificator = new List<CodeVerification>();

            var roles = await _roleContext.GetAllRoles();

            foreach (var role in roles)
            {
                string code = _saqueContext.GenerateCode();

                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até 60 SEGUNDOS para bater seu ponto.");
                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até 60 SEGUNDOS para bater seu ponto.");
                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até 60 SEGUNDOS para bater seu ponto.");
                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até 60 SEGUNDOS para bater seu ponto.");
                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até 60 SEGUNDOS para bater seu ponto.");

                PlayerCodeVerificator.Add(new CodeVerification(AddHour, FailNotification, role, code, _definitions));
            }
        }, null, 15_000, 3_600_000 + randomizer.Next(-120_000, 120_000));
        Timer chatTimer = new Timer(async _ =>
        {
            try
            {
                PlayerCodeVerificator = PlayerCodeVerificator.Where(x => x.roleControl != null).ToList();

                long fileSize = GetFileSize(path);

                if (fileSize > lastSize)
                {
                    List<Message> messages = ReadTail(path, UpdateLastFileSize(fileSize));

                    foreach (var message in messages)
                    {
                        await CommandForward(message);
                    }

                    hourlyTimer.Change(0, 3_600_000 + randomizer.Next(-120_000, 120_000));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }, null, 0, 500);

        await Task.Delay(-1, stoppingToken);
    }

    private async Task FailNotification(RoleAnswerControl roleControl)
    {
        await _serverContext.SendPrivateMessage(roleControl.Role.Id, "Você não digitou o código dentro do prazo limite. Tente novamente em 1 hora.");
        LogWriter.Write($"{roleControl.Role.Id} falhou em bater ponto");
    }
    private async Task AddHour(RoleAnswerControl roleControl)
    {
        await _roleContext.AddHour(roleControl.Role.Id);
    }

    private async Task CommandForward(Message message)
    {
        Task forwardCommand = message.Text.Trim() switch
        {
            string curMessage when curMessage.Contains("!ajuda") => HelpMessage(message),
            string curMessage when curMessage.Contains("!sacarhora") => DeliverReward(message),
            string curMessage when curMessage.Contains("!tophora") => GetHoursRanking(message.RoleID),
            string curMessage when curMessage.Contains("!itensdisponiveis") => SendItemsAvailable(message.RoleID),
            string curMessage when curMessage.Contains("!horas") => SendRoleHours(message.RoleID),
            string curMessage when curMessage.Length > 0 => TriggerCodeVerification(message),
            _ => Task.CompletedTask
        };

        await forwardCommand;
    }

    private async Task TriggerCodeVerification(Message message)
    {
        PlayerCodeVerificator
            ?.Where(x => (bool)(x.roleControl?.Role?.Id.Equals(message.RoleID)))
            ?.FirstOrDefault()
            ?.RoleAnswerTrigger(message.Text);
    }
    private async Task HelpMessage(Message message)
    {
        await _serverContext.SendPrivateMessage(message.RoleID, "• Digite    !sacarhora [item] [quantidade]   para sacar recompensas utilizando seu banco de horas.");
        await _serverContext.SendPrivateMessage(message.RoleID, "↑ OBS.: Sem os colchetes []");
        await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !itensdisponiveis para receber em PM todos os itens disponíveis para recompensa");

        if (_definitions.IsRankingAllowed)
            await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !tophora pra visualizar o ranking de quem possui mais horas logadas");

        await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !horas para receber seu banco de horas");
    }

    private async Task DeliverReward(Message message)
    {
        try
        {
            //Verifica se há recompensas elegíveis
            if (_definitions.ItemsReward.Count <= 0)
            {
                await _serverContext.SendPrivateMessage(message.RoleID, "Não há itens disponíveis para saque.");

                return;
            }

            //Retira o trigger !reward da mensagem escrita, restando o nome do item e a quantidade, esta se houver
            string sentence = message.Text.Trim().Replace("!sacarhora", default).Trim();

            //Captura a quantidade de item, se houver número
            int amount = sentence.Any(char.IsDigit) ? int.Parse(System.Text.RegularExpressions.Regex.Match(sentence, @"\d+").Value) : 1;

            //Retorna o item especifico filtrado a quantidade
            sentence = sentence.Replace(amount.ToString(), default).Trim();

            //Verifica se o jogador que acionou o trigger está cadastrado no ranking
            Role currentUser = await _roleContext.GetRoleFromId(message.RoleID);
            if (currentUser is null)
            {
                await _serverContext.SendPrivateMessage(message.RoleID, "Erro ao processar saque");
                return;
            }

            //Verifica a nomenclatura da escrita do item a ser resgatado, tratando espaços vazios, casing e acentos
            if (!_definitions.ItemsReward.Select(x => TypeTreatments(x.Name)).Contains(TypeTreatments(sentence)))
            {
                //Verifica se o jogador digitou algo, ou se digitou algo errado para montar uma mensagem de feedback
                string displayMessage = sentence.Trim().Length <= 1 ? "É necessário especificar o nome do item a ser recebido" : @$"O termo ""{sentence}"" digitado não está elegível para recompensa.";

                await _serverContext.SendPrivateMessage(message.RoleID, displayMessage);

                //Envia os itens disponíveis para recompensa
                await SendItemsAvailable(message.RoleID);

                return;
            }

            //Verifica a quantidade de itens resgatados para evitar overflow
            if (amount > 99999 || amount <= 0)
            {
                await _serverContext.SendPrivateMessage(message.RoleID, $"A quantidade resgatada precisa estar contida no intervalo entre 1 e 99.999");

                return;
            }

            //Seleciona o item escolhido pelo jogador
            Item itemChoosed = _definitions.ItemsReward.Where(x => TypeTreatments(x.Name).Contains(TypeTreatments(sentence))).FirstOrDefault();

            //Verifica se o custo do item multiplicado pela quantia desejada supera as horas que o jogador possui
            if (currentUser.LoggedHours < itemChoosed.HoursCost * amount)
            {
                await _serverContext.SendPrivateMessage(message.RoleID, @$"Você não tem horas suficientes para resgatar ""{sentence}"". Necessita de {itemChoosed.HoursCost * amount} horas.");

                return;
            }

            //Gera o item e entrega no correio
            await _serverContext.DeliverReward(itemChoosed, amount, message.RoleID);

            await _roleContext.ReduceHour(currentUser.Id, itemChoosed.HoursCost * amount);

            await _serverContext.SendPrivateMessage(message.RoleID, $"Sua recompensa foi entregue. Em sua Caixa de Correios deve haver {amount * itemChoosed.Amount}x {itemChoosed.Name}({itemChoosed.HoursCost * amount} horas). Te restam {currentUser.LoggedHours} horas.");

            await _saqueContext.Add(new Saque
            {
                ItemId = itemChoosed.Id,
                ItemCount = itemChoosed.Amount,
                OrderCount = amount,
                ItemName = itemChoosed.Name,
                HourCost = itemChoosed.HoursCost * amount,
                RoleName = currentUser.CharacterName,
                RoleId = currentUser.Id,
                Date = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            LogWriter.Write(ex.ToString());
        }
    }
    private string TypeTreatments(string source)
    {
        string treatedSource = RemoveDiacritics(source.Trim().ToLower().Replace('ç', 'c'));
        return treatedSource;
    }
    private async Task SendItemsAvailable(int RoleId)
    {
        try
        {
            foreach (var item in _definitions.ItemsReward)
            {
                await _serverContext.SendPrivateMessage(RoleId, $"Item: {item.Amount}x {item.Name}. Custo: {item.HoursCost} hora(s).");
            }
        }
        catch (Exception ex)
        {
            LogWriter.Write(ex.ToString());
        }
    }

    private async Task GetHoursRanking(int roleId)
    {
        if (_definitions.IsRankingAllowed)
        {
            double cooldown = DateTime.Now.Subtract(lastTopRank).TotalSeconds;

            if (lastTopRank.Year.Equals(1990) || cooldown > 30)
            {
                var ranking = await _roleContext.GetHoursRanking();

                ranking.ForEach(player => _serverContext.SendMessage(_definitions.Channel, $"{ranking.IndexOf(player) + 1}º lugar: {player.CharacterName}. Horas: {player.TotalHours}"));

                lastTopRank = DateTime.Now;
            }
            else
            {
                await _serverContext.SendPrivateMessage(roleId, $"O pedido está em tempo de espera. Tente novamente em {((cooldown - 30) * -1).ToString("0")} segundos.");
            }
        }
    }

    private List<Message> ReadTail(string filename, long offset)
    {
        try
        {
            byte[] bytes;

            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(offset * -1, SeekOrigin.End);

                bytes = new byte[offset];
                fs.Read(bytes, 0, (int)offset);
            }

            List<string> logs = Encoding.Default.GetString(bytes).Split("\n").Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

            List<Message> decodedMessages = _mFactory.GetMessages(logs);

            return decodedMessages;
        }
        catch (Exception ex)
        {
            LogWriter.Write(ex.ToString());
        }

        return default;
    }
    private long UpdateLastFileSize(long fileSize)
    {
        long difference = fileSize - lastSize;
        lastSize = fileSize;

        return difference;
    }
    private long GetFileSize(string fileName)
    {
        return new FileInfo(fileName).Length;
    }
    private string RemoveDiacritics(string text)
    {
        string formD = text.ToLower().Trim().Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char ch in formD)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task SendRoleHours(int roleID) => await _roleContext.SendHours(roleID);
}