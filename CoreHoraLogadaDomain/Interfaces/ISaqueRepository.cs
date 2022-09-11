using CoreHoraLogadaInfra.Models;
using System.Threading.Tasks;

namespace CoreHoraLogada.Infrastructure.Interfaces
{
    public interface ISaqueRepository
    {
        Task Add(Saque saque);
        string GenerateCode();
    }
}