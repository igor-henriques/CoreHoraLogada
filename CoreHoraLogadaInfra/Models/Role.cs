using System;
using System.ComponentModel.DataAnnotations;

namespace CoreHoraLogadaInfra.Models
{
    public class Role : ICloneable
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(30)]
        public string CharacterName { get; set; }
        public DateTime LastTimeCheck { get; set; }
        public int LoggedHours { get; set; }
        public int TotalHours { get; set; }

        public Role(Role role)
        {
            this.Id = role.Id;
            this.CharacterName = role.CharacterName;
            this.LastTimeCheck = role.LastTimeCheck;
            this.LoggedHours = role.LoggedHours;
            this.TotalHours = role.TotalHours;
        }

        public Role()
        {

        }

        public object Clone()
        {
            return new Role(this);
        }
    }
}
