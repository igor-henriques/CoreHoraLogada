using PWToolKit;
using PWToolKit.API.GDeliveryd;
using PWToolKit.API.GProvider;
using PWToolKit.Enums;
using PWToolKit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using PWToolKit.API.Gamedbd;
using CoreHoraLogadaInfra.Models;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;

namespace CoreHoraLogadaDomain.Repository
{
    public interface IServerRepository
    {
        Task SendPrivateMessage(int roleId, string message);
        Task SendMessage(BroadcastChannel channel, string message);
        Task<GRoleData> GetRoleByID(int roleId);
        Task<GRoleData> GetRoleByName(string characterName);
        GMPlayerInfo[] GetAllRoles();
        Task<int> GetRoleIdByName(string characterName);
        Task<List<GRoleData>> GetRolesFromAccount(int userId);
        Task SendMail(int roleId, string title, string message, GRoleInventory item);
        string GetLogsPath();
        PwVersion GetPwVersion();
        string GetRoleNameByID(int roleId);
        Task DeliverReward(Item item, int amount, int roleId);
        Task GiveCash(int accountId, int cashAmount);
    }
    public class ServerRepository : IServerRepository
    {
        private readonly ServerConnection _server;

        private readonly Definitions _definitions;

        public ServerRepository(ServerConnection server, Definitions definitions)
        {
            this._server = server;
            this._definitions = definitions;

            PWGlobal.UsedPwVersion = _server.PwVersion;
        }

        public async Task<GRoleData> GetRoleByID(int roleId)
        {
            GRoleData roleData = GetRoleData.Get(_server.gamedbd, roleId);
            return roleData;
        }

        public async Task<GRoleData> GetRoleByName(string characterName)
        {
            GRoleData roleData = await GetRoleByID(GetRoleId.Get(_server.gamedbd, characterName));
            return roleData;
        }

        public async Task SendPrivateMessage(int roleId, string message)
        {
            try
            {
                await Task.Run(() => PrivateChat.Send(_server.gdeliveryd, roleId, message));
            }
            catch (Exception e)
            {
                LogWriter.Write(e.ToString());
            }
        }
        public async Task<List<GRoleData>> GetRolesFromAccount(int userId)
        {
            List<int> idRoles = GetUserRoles.Get(_server.gamedbd, userId).Select(x => x.Item1).ToList();

            List<GRoleData> roles = new List<GRoleData>();

            idRoles.ForEach(x => roles.Add(GetRoleData.Get(_server.gamedbd, x)));

            return roles;
        }

        public async Task SendMail(int roleId, string title, string message, GRoleInventory item)
        {
            try
            {
                await Task.Run(() => SysSendMail.Send(_server.gdeliveryd, roleId, title, message, item));
            }
            catch (Exception e)
            {
                LogWriter.Write(e.ToString());
            }
        }

        public string GetLogsPath()
        {
            return _server.logsPath;
        }

        public PwVersion GetPwVersion()
        {
            return _server.PwVersion;
        }

        public async Task<int> GetRoleIdByName(string characterName)
        {
            return await Task.Run(() => GetRoleId.Get(_server.gamedbd, characterName));
        }

        public string GetRoleNameByID(int roleId)
        {
            return GetRoleBase.Get(_server.gamedbd, roleId).Name;
        }

        public async Task DeliverReward(Item itemChoosed, int orderAmount, int roleId)
        {
            GRoleInventory item = new GRoleInventory()
            {
                Id = itemChoosed.Id,
                MaxCount = 99999,
                Pos = GetRolePocket.Get(_server.gamedbd, roleId).Items.Length + 1,
                Proctype = int.Parse(itemChoosed.Proctype),
                Octet = itemChoosed.Octet,
                Mask = int.Parse(itemChoosed.Mask),                
            };

            //Estrutura condicional para determinar se o stack do item é maior/igual à quantidade requisitada
            if (itemChoosed.Stack >= orderAmount * itemChoosed.Amount)
            {
                item.Count = orderAmount * itemChoosed.Amount;

                await SendMail(roleId, "RECOMPENSA DE HORA LOGADA", "Parabéns pelo empenho!", item);
            }
            else
            {
                item.Count = 1;

                for (int i = 0; i < orderAmount; i++)
                {
                    await SendMail(roleId, "RECOMPENSA DE HORA LOGADA", "Parabéns pelo empenho!", item);
                }
            }
        }
        public async Task GiveCash(int accountId, int cashAmount)
        {
            await Task.Run(() => DebugAddCash.Add(_server.gamedbd, accountId, cashAmount * 100));
        }

        public GMPlayerInfo[] GetAllRoles()
        {
            return GMListOnlineUser.Get(_server.gdeliveryd);            
        }
        public async Task SendMessage(BroadcastChannel channel, string message)
        {
            try
            {
                await Task.Run(() => ChatBroadcast.Send(_server.gprovider, channel, $"{((channel.Equals(BroadcastChannel.System) && _server.PwVersion.Equals(PwVersion.V155)) ? _definitions.MessageColor : default)}{message}"));
            }
            catch (Exception e)
            {
                LogWriter.Write(e.ToString());
            }
        }
    }
}
