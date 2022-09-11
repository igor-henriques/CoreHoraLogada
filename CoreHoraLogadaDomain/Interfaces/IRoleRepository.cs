using CoreHoraLogadaInfra.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreHoraLogada.Infrastructure.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role> AddByID(int role);
        Task AddHour(int roleId);
        Task<List<Role>> GetAllRoles();
        Task<List<Role>> GetHoursRanking();
        Task<Role> GetRoleFromId(int roleId);
        Task ReduceHour(int roleId, int hours);
        Task SendHours(int roleId);
    }
}