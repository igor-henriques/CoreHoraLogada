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
        public SaqueRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task Add(Saque saque)
        {
            await _context.Saque.AddAsync(saque);
            await _context.SaveChangesAsync();

            LogWriter.Write($"{saque.Role.CharacterName}({saque.Role.Id}) sacou {saque.ItemCount * saque.OrderCount}x {saque.ItemName}({saque.ItemId}) às {saque.Date}, gastando {saque.HourCost} horas do seu banco.");
        }

        public string GenerateCode()
        {
            string alphabet = "1Q1E52TY832AS67FG3JK7X4C48BNMW9P5HVR6DZU9";

            string randomGuid = string.Empty;

            for (int i = 0; i < 8; i++)
            {
                int index = new Random().Next(0, alphabet.Length);
                randomGuid += alphabet[index];
            }

            return randomGuid;
        }
    }
}
