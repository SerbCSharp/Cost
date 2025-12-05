using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Payments
{
    public class PaymentDecryption // Расшифровка платежа
    {
        public string Ref_Key { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }

        [JsonPropertyName("СуммаПлатежа")]
        public decimal PaymentAmount { get; set; }

        [JsonPropertyName("СуммаНДС")]
        public decimal PaymentNDSAmount { get; set; }
    }
}
