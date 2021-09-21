namespace CoreHoraLogadaInfra.Models
{
    public record Item
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public int Amount { get; init; }
        public int Stack { get; init; }
        public string Octet { get; init; }
        public string Proctype { get; init; }
        public string Mask { get; init; }
        public int HoursCost { get; init; }
    }
}
