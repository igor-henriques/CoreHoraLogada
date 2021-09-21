using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreHoraLogadaInfra.Models
{
    public record Saque
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public int OrderCount { get; set; }
        [MaxLength(30)]
        public string ItemName { get; set; }
        public int HourCost { get; set; }
        public DateTime Date { get; set; }
    }
}
