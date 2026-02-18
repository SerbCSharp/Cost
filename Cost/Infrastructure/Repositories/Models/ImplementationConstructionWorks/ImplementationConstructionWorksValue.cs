using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ImplementationConstructionWorks
{
    public class ImplementationConstructionWorksValue
    {
        [JsonPropertyName("Ref_Key")]
        public string ReceiptId { get; set; }
        public DateTime Date { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }
    }
}
