using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.BuyerPaymentInvoice
{
    public class BuyerPaymentInvoiceValue
    {
        [JsonPropertyName("Ref_Key")]
        public string BuyerPaymentInvoiceId { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }
    }
}
