using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Payments
{
    public class PaymentsValue
    {
        public DateTime Date { get; set; }

        public bool Posted { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }

        [JsonPropertyName("РасшифровкаПлатежа")]
        public PaymentDecryption[] PaymentDecryption { get; set; }

        [JsonPropertyName("Ref_Key")]
        public string PaymentId { get; set; }
        public bool? DeletionMark { get; set; }
        public string PaymentDecryptionId { get; set; }
    }
}
