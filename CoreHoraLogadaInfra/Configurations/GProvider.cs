using PWToolKit.Packets;

namespace CoreRankingInfra.Server
{
    public record GProvider : IPwDaemonConfig
    {
        public string Host { get; }
        public int Port { get; }

        public GProvider(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
