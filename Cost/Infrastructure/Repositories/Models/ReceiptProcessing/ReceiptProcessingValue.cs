using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptProcessing
{
    public class ReceiptProcessingValue
    {
        public DateTime Date { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }
    }
}
