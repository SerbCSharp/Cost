using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.BuyerPaymentInvoice
{
    public class BuyerPaymentInvoice
    {
        [JsonPropertyName("value")]
        public BuyerPaymentInvoiceValue[] Value { get; set; }
    }
}
