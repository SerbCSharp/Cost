using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Selling
{
    public class SellingValue
    {
        public DateTime Date { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }
    }
}
