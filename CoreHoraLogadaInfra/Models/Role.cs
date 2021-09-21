using System;
using System.ComponentModel.DataAnnotations;

namespace CoreHoraLogadaInfra.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(30)]
        public string CharacterName { get; set; }
        public DateTime LastTimeCheck { get; set; }
        public int LoggedHours { get; set; }
        public int TotalHours { get; set; }
    }
}
