using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using Microsoft.EntityFrameworkCore;
using PWToolKit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreHoraLogadaDomain.Repository
{
    public interface IRoleRepository
    {
        Task<Role> AddByRankingModel(Role role);
        Task<Role> AddByID(int roleId);
        Task<List<Role>> GetAllRoles();
        Task<List<Role>> GetHoursRanking();
        Task UpdateTimeCheck(int roleId);
        Task SendHours(int roleId);
        Task RemoveByModel(Role role);
        Task RemoveByID(int roleId);
        Task Update(Role role);
        Task<Role> GetRoleFromName(string characterName);
        Task<Role> GetRoleFromId(int roleId);
        Task SaveChangesAsync();
        string ConvertClassToGameStructure(string name);
        string ConvertClassFromGameStructure(string name);
        Task AddHour(int roleId);
        Task ReduceHour(int roleId, int hours);
    }

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

        public async Task<Role> AddByRankingModel(Role role)
        {
            await _context.Role.AddAsync(role);

            await _context.SaveChangesAsync();

            return await _context.Role.Where(x => x.CharacterName.Equals(role.CharacterName)).FirstOrDefaultAsync();
        }

        public async Task<Role> AddByID(int role)
        {
            GRoleData roleBase = await _serverContext.GetRoleByID(role);

            Role roleData = new Role
            {
                Id = roleBase.GRoleBase.Id,
                CharacterName = roleBase.GRoleBase.Name,
                LastTimeCheck = DateTime.Now,
                LoggedHours = 0
            };

            await _context.Role.AddAsync(roleData);

            await _context.SaveChangesAsync();

            LogWriter.Write($"O personagem {roleData.CharacterName} foi incluído no Ranking via ID.");

            return await _context.Role.Where(x => x.Id.Equals(roleBase.GRoleBase.Id)).FirstOrDefaultAsync();
        }
        public async Task RemoveByModel(Role role)
        {
            _context.Role.Remove(role);

            LogWriter.Write($"O personagem {role.CharacterName} foi excluído do Ranking via DbModel.");

            await _context.SaveChangesAsync();
        }

        public async Task RemoveByID(int roleId)
        {
            Role roleToDelete = await _context.Role.FindAsync(roleId);

            if (roleToDelete != null)
            {
                _context.Role.Remove(roleToDelete);

                LogWriter.Write($"O personagem {roleToDelete.CharacterName} foi excluído do Ranking via ID.");
            }

            await _context.SaveChangesAsync();
        }

        public async Task Update(Role role)
        {
            Role curRole = await _context.Role.Where(x => x.Id.Equals(role.Id)).FirstOrDefaultAsync();

            if (curRole is not null)
            {
                curRole = role;

                LogWriter.Write($"O personagem {curRole.CharacterName} foi atualizado no Ranking via DbModel.");
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Traduz as iniciais de cada classe para o nome original da classe, utilizado na estrutura do jogo. Ex.: EP = Priest
        /// </summary>
        /// <param name="classInitials">Sigla que representa a classe</param>
        /// <returns></returns>
        public string ConvertClassToGameStructure(string classInitials)
        {
            try
            {
                return translateClassName.Where(x => x.Value.ToUpper().Equals(classInitials.Trim().ToUpper())).Select(y => y.Key).FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }

        /// <summary>
        /// Traduz o nome de cada classe utilizada na estrutura do jogo para o conhecido comumente.
        /// </summary>
        /// <param name="classFullName">Nome inteiro da classe</param>
        /// <returns></returns>
        public string ConvertClassFromGameStructure(string classFullName)
        {
            try
            {
                return translateClassName.Where(x => x.Key.ToUpper().Equals(classFullName.Trim().ToUpper())).Select(y => y.Value).FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogWriter.Write(ex.ToString());
            }

            return default;
        }

        public async Task<List<Role>> GetAllRoles()
        {
            //Retorna da base do game todos os personagens online
            var roles = _serverContext.GetAllRoles();

            //Retorna os Ids da lista acima
            var rolesId = roles.Select(x => x.RoleId);

            //Retorna da tabela do software todos os personagens que estão online no game
            var fromDbRoles = await _context.Role.Where(x => rolesId.Contains(x.Id)).ToListAsync();

            //Retira a diferença entre o retorno da tabela do software e da base do game
            var missingRoles = rolesId.Except(fromDbRoles.Select(x => x.Id).ToList()).ToList();

            if (missingRoles.Count > 0)
            {
                //Adiciona à tabela do software a diferença acima
                missingRoles.ForEach(async x => await AddByID(x));

                //Adiciona à consulta os registros acima que faltaram
                fromDbRoles.AddRange(await _context.Role.Where(x => missingRoles.Contains(x.Id)).ToListAsync());
            }

            return fromDbRoles;
        }

        public async Task AddHour(int roleId)
        {
            var role = await this.GetRoleFromId(roleId);

            role.LoggedHours++;
            role.TotalHours++;

            await _context.SaveChangesAsync();

            await _serverContext.SendPrivateMessage(roleId, $"Hora confirmada com sucesso. Seu banco de horas: {role.LoggedHours}");
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
        public async Task<Role> GetRoleFromName(string characterName) => await _context.Role.Where(x => x.CharacterName.Equals(characterName)).FirstOrDefaultAsync();
        public async Task<List<Role>> GetHoursRanking() => await _context.Role.OrderBy(x => x.LoggedHours).Take(_definitions.PlayersOnRanking).ToListAsync();
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();        

        public async Task UpdateTimeCheck(int roleId)
        {
            var currentRole = await GetRoleFromId(roleId);

            currentRole.LastTimeCheck = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}