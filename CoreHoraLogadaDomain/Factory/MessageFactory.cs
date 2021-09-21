using CoreHoraLogadaDomain.Repository;
using CoreHoraLogadaInfra.Models;
using CoreRankingInfra.Model;
using PWToolKit.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreHoraLogadaDomain.Factory
{
    public record MessageFactory
    {
        private readonly IServerRepository _serverContext;
        public MessageFactory(IServerRepository serverContext)
        {
            this._serverContext = serverContext;
        }

        public Message GetMessage(string log)
        {
            if (log.Contains("src=") && !log.Contains("src=-1") && !log.Contains("whisper"))
            {
                if (int.TryParse(System.Text.RegularExpressions.Regex.Match(log, @"chl=([0-9]*)").Value.Replace("chl=", ""), out int channel))
                {
                    //Se conseguir dar parse em RoleID e o canal de envio da mensagem estiver contido dentro da lista de canais permitidos
                    if (int.TryParse(System.Text.RegularExpressions.Regex.Match(log, @"src=([0-9]*)").Value.Replace("src=", ""), out int roleId))
                    {
                        string text = Encoding.Unicode.GetString(Convert.FromBase64String(System.Text.RegularExpressions.Regex.Match(log, @"msg=([\s\S]*)").Value.Replace("msg=", "")));

                        Message newMessage = new Message(
                            (BroadcastChannel)channel,
                            roleId,
                            _serverContext.GetRoleNameByID(roleId),
                            text);

                        return newMessage;
                    }
                }                
            }

            return default;
        }
        public List<Message> GetMessages(List<string> log)
        {
            return log.Select(log => GetMessage(log)).Where(x => x != default).ToList();
        }
    }
}