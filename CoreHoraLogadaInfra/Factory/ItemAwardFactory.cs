using CoreHoraLogadaInfra.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace CoreHoraLogadaInfra.Configurations
{
    public class ItemAwardFactory
    {
        private List<Item> ItemsAward = new List<Item>();
        public ItemAwardFactory()
        {
            JObject jsonNodes = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("./Configurations/ItensAward.json"));

            foreach (var item in jsonNodes)
            {
                Item Item = new Item
                {
                    Id = int.Parse(item.Key),
                    Name = item.Value["NOME"].ToObject<string>(),
                    Amount = item.Value["QUANTIA"].ToObject<int>(),
                    Stack = item.Value["STACK"].ToObject<int>(),
                    Octet = item.Value["OCTET"].ToObject<string>(),
                    Proctype = item.Value["PROCTYPE"].ToObject<string>(),
                    Mask = item.Value["MASK"].ToObject<string>(),
                    HoursCost = item.Value["CUSTO EM HORAS"].ToObject<int>()
                };

                ItemsAward.Add(Item);
            }
        }

        public List<Item> Get()
        {
            return ItemsAward;
        }
    }
}