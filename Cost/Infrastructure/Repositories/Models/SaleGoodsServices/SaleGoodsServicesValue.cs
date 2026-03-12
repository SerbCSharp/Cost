using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.SaleGoodsServices
{
    public class SaleGoodsServicesValue
    {
        public DateTime Date { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }
    }
}
