using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DepositToCurrentAccount
{
    public class PaymentDetails
    {
        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("СуммаПлатежа")]
        public decimal PaymentAmount { get; set; }

        [JsonPropertyName("СчетНаОплату_Key")]
        public string PaymentInvoiceId { get; set; }
    }
}
