using CoreHoraLogadaDomain;
using CoreHoraLogadaDomain.Factory;
using CoreHoraLogadaDomain.Repository;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using CoreRankingInfra.Model;
using PWToolKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CoreHoraLogada.Watchers
{
    public class ChatWatch
    {
        private readonly IServiceProvider _services;

        private readonly IServerRepository _serverContext;

        private readonly IRoleRepository _roleContext;

        private readonly ISaqueRepository _saqueContext;

        private readonly Definitions _definitions;

        private readonly MessageFactory _mFactory;

        private readonly string path;

        private readonly System.Timers.Timer _chatWatch = new Timer(500);

        private readonly System.Timers.Timer _hourlyWatch = new Timer(15000);

        private List<CodeVerification> PlayerCodeVerificator;

        private long lastSize;

        private DateTime lastTopRank;

        public ChatWatch(IServiceProvider services)
        {
            try
            {
                this._services = services;

                this.lastTopRank = new DateTime(1990, 1, 1);

                this._mFactory = _services.GetService(typeof(MessageFactory)) as MessageFactory;

                this._roleContext = _services.GetService(typeof(IRoleRepository)) as IRoleRepository;

                this._serverContext = _services.GetService(typeof(IServerRepository)) as IServerRepository;

                this._saqueContext = _services.GetService(typeof(ISaqueRepository)) as ISaqueRepository;

                this._definitions = _services.GetService(typeof(Definitions)) as Definitions;

                this.path = $"{Path.Combine(_serverContext.GetLogsPath(), "world2.chat")}";

                lastSize = GetFileSize(path);

                PWGlobal.UsedPwVersion = _serverContext.GetPwVersion();

                PlayerCodeVerificator = new List<CodeVerification>();

                _chatWatch.Elapsed += ChatTick;

                _chatWatch.Start();

                _hourlyWatch.Elapsed += HourlyTick;

                _hourlyWatch.Start();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

        }

        private async void HourlyTick(object sender, ElapsedEventArgs e)
        {
            PlayerCodeVerificator = new List<CodeVerification>();

            var _serverContext = _services.GetService(typeof(IServerRepository)) as IServerRepository;            
            var _roleContext = _services.GetService(typeof(IRoleRepository)) as IRoleRepository;

            var roles = await _roleContext.GetAllRoles();

            foreach (var role in roles)
            {
                string code = _saqueContext.GenerateCode();

                await _serverContext.SendPrivateMessage(role.Id, $"1 hora se passou! Digite o código {code} em até {_definitions.TimeToAnswer} SEGUNDOS para bater seu ponto.");

                PlayerCodeVerificator.Add(new CodeVerification(AddHour, FailNotification, role, code, _definitions));
            }

            _hourlyWatch.Interval = 3600000 + new Random().Next(-120000, 120000);
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

        private void ChatTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                PlayerCodeVerificator = PlayerCodeVerificator.Where(x => x.roleControl != null).ToList();

                long fileSize = GetFileSize(path);

                if (fileSize > lastSize)
                {
                    List<Message> messages = ReadTail(path, UpdateLastFileSize(fileSize));

                    messages.Where(x => x != default).ToList().ForEach(async message => await CommandForward(message));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }
        }
        private async Task CommandForward(Message message)
        {
            //Redireciona à função respectiva do trigger acionado via chat
            Task forwardCommand = message.Text.Trim() switch
            {        
                string curMessage when curMessage.Contains("!ajuda") => HelpMessage(message),
                string curMessage when curMessage.Contains("!sacarhora") => DeliverReward(message),
                string curMessage when curMessage.Contains("!tophora") => GetHoursRanking(message.RoleID),
                string curMessage when curMessage.Contains("!itensdisponiveis") => SendItemsAvailable(message.RoleID),
                string curMessage when curMessage.Contains("!horas") => SendRoleHours(message.RoleID),
                string curMessage when curMessage.Length > 0 => TriggerCodeVerification(message),
                _ => Task.Run(() => { })
            };

            await forwardCommand;
        }
                
        private async Task TriggerCodeVerification(Message message)
        {
            PlayerCodeVerificator.Where(x => (bool)(x.roleControl?.Role.Id.Equals(message.RoleID))).FirstOrDefault()?.RoleAnswerTrigger(message.Text);
        }
        private async Task HelpMessage(Message message)
        {
            await _serverContext.SendPrivateMessage(message.RoleID, "• Digite    !sacarhora [item] [quantidade]   para sacar recompensas utilizando seu banco de horas.");
            await _serverContext.SendPrivateMessage(message.RoleID, "↑ OBS.: Sem os colchetes []");
            await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !itensdisponiveis para receber em PM todos os itens disponíveis para recompensa");
            await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !tophora pra visualizar o ranking de quem possui mais horas logadas");
            await _serverContext.SendPrivateMessage(message.RoleID, "• Digite !horas para receber seu banco de horas");
        }

        private async Task DeliverReward(Message message)
        {
            try
            {
                //Retira o trigger !reward da mensagem escrita, restando o nome do item e a quantidade, esta se houver
                string sentence = message.Text.Trim().Replace("!sacarhora", default).Trim();

                //Captura a quantidade de item, se houver número
                int amount = sentence.Any(char.IsDigit) ? int.Parse(System.Text.RegularExpressions.Regex.Match(sentence, @"\d+").Value) : 1;

                //Retorna o item especifico filtrado a quantidade
                sentence = sentence.Replace(amount.ToString(), default).Trim();

                //Verifica se há recompensas elegíveis
                if (_definitions.ItemsReward.Count <= 0)
                {
                    await _serverContext.SendPrivateMessage(message.RoleID, "Não há itens disponíveis para saque.");

                    return;
                }

                //Verifica se o jogador que acionou o trigger está cadastrado no ranking
                Role currentUser = await _roleContext.GetRoleFromId(message.RoleID);
                if (currentUser is null)
                {
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

                await _serverContext.SendPrivateMessage(message.RoleID, $"Sua recompensa foi entregue. Em sua Caixa de Correios deve haver {amount * itemChoosed.Amount}x {itemChoosed.Name}({itemChoosed.HoursCost * amount} pontos). Te restam {currentUser.LoggedHours} pontos.");

                await _roleContext.SaveChangesAsync();

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

                List<string> logs = Encoding.Default.GetString(bytes).Split(new string[] { "\n" }[0]).Where(x => !string.IsNullOrEmpty(x.Trim())).ToList();

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
}