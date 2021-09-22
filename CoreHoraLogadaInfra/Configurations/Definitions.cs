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
        public BroadcastChannel Channel { get; set; }
        public string MessageColor { get; set; }        
        public int PlayersOnRanking { get; set; }
        public List<Item> ItemsReward { get; set; }
        public int CodeLength { get; set; }
        public int TimeToAnswer { get; set; }
        public bool IsRankingAllowed { get; set; }

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
