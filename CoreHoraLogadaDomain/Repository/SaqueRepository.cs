using CoreHoraLogada.Infrastructure.Interfaces;
using CoreHoraLogadaInfra.Configurations;
using CoreHoraLogadaInfra.Data;
using CoreHoraLogadaInfra.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CoreHoraLogadaDomain.Repository;

public class SaqueRepository : ISaqueRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Definitions _definitions;
    private readonly Random randomizer;
    private const string alphabet = "1Q1E52TY832AS67FG3JK7X4C48BNMW9P5HVR6DZU9";

    public SaqueRepository(ApplicationDbContext context, Definitions definitions)
    {
        this._context = context;
        this._definitions = definitions;
        this.randomizer = new Random();
    }

    public async Task Add(Saque saque)
    {
        _context.Saque.Add(saque);
        await _context.SaveChangesAsync();

        LogWriter.Write($"{saque.RoleName}({saque.RoleId}) sacou {saque.ItemCount * saque.OrderCount}x {saque.ItemName}({saque.ItemId}) às {saque.Date}, gastando {saque.HourCost} horas do seu banco.");
    }

    public string GenerateCode()
    {
        StringBuilder randomGuid = new StringBuilder();

        for (int i = 0; i < _definitions.CodeLength; i++)
        {
            int index = randomizer.Next(0, alphabet.Length);
            randomGuid.Append(alphabet[index]);
        }

        return randomGuid.ToString();
    }
}
