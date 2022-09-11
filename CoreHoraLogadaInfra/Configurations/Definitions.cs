using CoreHoraLogadaInfra.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWToolKit.Enums;
using System.Collections.Generic;
using System.IO;

namespace CoreHoraLogadaInfra.Configurations
{
    public record Definitions
    {
        public BroadcastChannel Channel { get; }
        public string MessageColor { get; }
        public int PlayersOnRanking { get; }
        public List<Item> ItemsReward { get; }
        public int CodeLength { get; }
        public int TimeToAnswer { get; }
        public bool IsRankingAllowed { get; }

        public Definitions(ItemAwardFactory itemFactory)
        {
            JObject jsonNodes = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("./Configurations/Definitions.json"));

            this.Channel = (BroadcastChannel)jsonNodes["CANAL"].ToObject<int>();
            this.MessageColor = jsonNodes["COR DA MENSAGEM"].ToObject<string>();
            this.PlayersOnRanking = jsonNodes["QUANTIDADE DE JOGADORES NO TOPRANK"].ToObject<int>();
            this.CodeLength = jsonNodes["QUANTIDADE DE CARACTERES NO CÓDIGO"].ToObject<int>();
            this.TimeToAnswer = jsonNodes["TEMPO PARA RESPONDER"].ToObject<int>();
            this.IsRankingAllowed = jsonNodes["PERMITIDO RANKING"].ToObject<bool>();
            this.ItemsReward = itemFactory.Get();
        }
    }
}
