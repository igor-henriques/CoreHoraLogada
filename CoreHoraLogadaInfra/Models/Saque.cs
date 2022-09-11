using System;
using System.ComponentModel.DataAnnotations;

namespace CoreHoraLogadaInfra.Models;

public sealed record Saque
{
    [Key]
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }
    public int OrderCount { get; set; }
    [MaxLength(30)]
    public string ItemName { get; set; }
    public int HourCost { get; set; }
    public DateTime Date { get; set; }
}
