using CoreHoraLogada.Infrastructure.Interfaces;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using Microsoft.EntityFrameworkCore;
using PWToolKit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreHoraLogadaDomain.Repository;

public class RoleRepository : IRoleRepository
{
    private readonly Dictionary<string, string> translateClassName = new Dictionary<string, string>();
    private readonly ApplicationDbContext _context;
    private readonly Definitions _definitions;
    private readonly IServerRepository _serverContext;
    public RoleRepository(ApplicationDbContext context, Definitions rankingDefinitions, IServerRepository serverContext)
    {
        #region POPULATING_TRANSLATOR_DICTIONARY
        translateClassName.Add("Warrior", "WR");
        translateClassName.Add("Mage", "MG");
        translateClassName.Add("Shaman", "PSY");
        translateClassName.Add("Druid", "WF");
        translateClassName.Add("Werewolf", "WB");
        translateClassName.Add("Assassin", "MC");
        translateClassName.Add("Archer", "EA");
        translateClassName.Add("Priest", "EP");
        translateClassName.Add("Guardian", "SK");
        translateClassName.Add("Mystic", "MS");
        translateClassName.Add("Reaper", "TM");
        translateClassName.Add("Ghost", "RT");
        #endregion

        this._context = context;
        this._definitions = rankingDefinitions;
        this._serverContext = serverContext;
    }

    public async Task<Role> AddByID(int role)
    {
        GRoleData roleBase = await _serverContext.GetRoleByID(role);

        Role roleData = new Role
        {
            Id = roleBase.GRoleBase.Id,
            CharacterName = roleBase.GRoleBase.Name,
            LastTimeCheck = DateTime.Now,
            LoggedHours = 0,
            TotalHours = 0
        };

        _context.Role.Add(roleData);

        await _context.SaveChangesAsync();

        LogWriter.Write($"O personagem {roleData.CharacterName} foi incluído no Ranking via ID.");

        return roleData;
    }

    public async Task<List<Role>> GetAllRoles()
    {
        //Retorna da base do game todos os personagens online
        var roles = _serverContext.GetAllRoles();

        //Retorna os Ids da lista acima
        var rolesId = roles.Select(x => x.RoleId);

        //Retorna da tabela do software todos os personagens que estão online no game
        var fromDbRoles = await _context.Role.AsNoTracking().Where(x => rolesId.Contains(x.Id)).ToListAsync();

        //Retira a diferença entre o retorno da tabela do software e da base do game
        var missingRoles = rolesId.Except(fromDbRoles.Select(x => x.Id));

        foreach (var role in missingRoles)
        {
            await AddByID(role);
        }

        return fromDbRoles;
    }

    public async Task AddHour(int roleId)
    {
        var role = await this.GetRoleFromId(roleId);

        role.LoggedHours++;
        role.TotalHours++;
        role.LastTimeCheck = DateTime.Now;

        if (await _context.SaveChangesAsync() > 0)
        {
            await _serverContext.SendPrivateMessage(roleId, $"Hora confirmada com sucesso. Seu banco de horas: {role.LoggedHours}");
            LogWriter.Write($"{role.CharacterName}({role.Id}) bateu ponto. Banco: {role.LoggedHours}");
        }
    }

    public async Task ReduceHour(int roleId, int hours)
    {
        var role = await GetRoleFromId(roleId);

        role.LoggedHours -= hours;

        await _context.SaveChangesAsync();
    }
    public async Task SendHours(int roleId)
    {
        var currentRole = await GetRoleFromId(roleId);

        await _serverContext.SendPrivateMessage(roleId, $"Horas totais(ranking): {currentRole.TotalHours}");
        await _serverContext.SendPrivateMessage(roleId, $"Horas para gastar(banco de horas): {currentRole.LoggedHours}");
    }
    public async Task<Role> GetRoleFromId(int roleId) => await _context.Role.FindAsync(roleId);
    public async Task<List<Role>> GetHoursRanking() => await _context.Role.OrderBy(x => x.LoggedHours).Take(_definitions.PlayersOnRanking).ToListAsync();
}