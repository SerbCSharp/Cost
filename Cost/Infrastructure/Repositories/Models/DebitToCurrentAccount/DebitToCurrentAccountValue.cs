using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebitToCurrentAccount
{
    public class DebitToCurrentAccountValue
    {
        [JsonPropertyName("Ref_Key")]
        public string PaymentId { get; set; }
        public DateTime Date { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal PaymentAmount { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("РасшифровкаПлатежа")]
        public PaymentDetails[] PaymentDetails { get; set; }

        [JsonPropertyName("НазначениеПлатежа")]
        public string PaymentPurpose { get; set; }

        [JsonPropertyName("ВидОперации")]
        public string TypeOperation { get; set; }
    }
}
