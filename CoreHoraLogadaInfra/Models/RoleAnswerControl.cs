using System.Threading;

namespace CoreHoraLogadaInfra.Models
{
    public class RoleAnswerControl
    {
        public Role Role { get; set; }
        public string Code { get; set; }
        public string LastAnswer { get; set; }
        public Timer RoleTimer { get; set; }
    }
}
