using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using System;
using System.Threading.Tasks;

namespace CoreHoraLogadaDomain.Repository
{
    public interface ISaqueRepository
    {
        string GenerateCode();
        Task Add(Saque saque);
    }
    public class SaqueRepository : ISaqueRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly Definitions _definitions;
        public SaqueRepository(ApplicationDbContext context, Definitions definitions)
        {
            this._context = context;
            this._definitions = definitions;
        }
        public async Task Add(Saque saque)
        {
            await _context.Saque.AddAsync(saque);
            _context.SaveChanges();

            LogWriter.Write($"{saque.RoleName}({saque.RoleId}) sacou {saque.ItemCount * saque.OrderCount}x {saque.ItemName}({saque.ItemId}) às {saque.Date}, gastando {saque.HourCost} horas do seu banco.");
        }

        public string GenerateCode()
        {
            string alphabet = "1Q1E52TY832AS67FG3JK7X4C48BNMW9P5HVR6DZU9";

            string randomGuid = string.Empty;

            for (int i = 0; i < _definitions.CodeLength; i++)
            {
                int index = new Random().Next(0, alphabet.Length);
                randomGuid += alphabet[index];
            }

            return randomGuid;
        }
    }
}
