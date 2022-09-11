using CoreHoraLogadaInfra.Models;
using PWToolKit;
using PWToolKit.Enums;
using PWToolKit.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreHoraLogada.Infrastructure.Interfaces
{
    public interface IServerRepository
    {
        Task DeliverReward(Item itemChoosed, int orderAmount, int roleId);
        GMPlayerInfo[] GetAllRoles();
        string GetLogsPath();
        PwVersion GetPwVersion();
        Task<GRoleData> GetRoleByID(int roleId);
        Task<GRoleData> GetRoleByName(string characterName);
        Task<int> GetRoleIdByName(string characterName);
        string GetRoleNameByID(int roleId);
        Task<List<GRoleData>> GetRolesFromAccount(int userId);
        Task GiveCash(int accountId, int cashAmount);
        Task SendMail(int roleId, string title, string message, GRoleInventory item);
        Task SendMessage(BroadcastChannel channel, string message);
        Task SendPrivateMessage(int roleId, string message);
    }
}