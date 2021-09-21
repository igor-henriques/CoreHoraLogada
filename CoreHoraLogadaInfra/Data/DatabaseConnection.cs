namespace CoreHoraLogadaInfra.Data
{
    public record DatabaseConnection
    {
        public string HOST { get; init; }
        public string DB { get; init; }
        public string USER { get; init; }
        public int PORT { get; init; }
        public string PASSWORD { get; init; }        
    }
}
