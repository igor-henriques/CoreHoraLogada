using PWToolKit.Packets;

namespace CoreRankingInfra.Server
{
    public record Gamedbd : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public Gamedbd(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
