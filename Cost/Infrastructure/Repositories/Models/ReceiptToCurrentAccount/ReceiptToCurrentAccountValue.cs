using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptToCurrentAccount
{
    public class ReceiptToCurrentAccountValue
    {
        public DateTime Date { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }
    }
}
