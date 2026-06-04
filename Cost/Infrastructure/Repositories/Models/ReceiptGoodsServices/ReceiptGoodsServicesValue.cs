using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptGoodsServices
{
    public class ReceiptGoodsServicesValue
    {
        public DateTime Date { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("Товары")]
        public Good[] Goods { get; set; }

    }
}
