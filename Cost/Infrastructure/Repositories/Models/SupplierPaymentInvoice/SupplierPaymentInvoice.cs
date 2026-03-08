using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.SupplierPaymentInvoice
{
    public class SupplierPaymentInvoice
    {
        [JsonPropertyName("value")]
        public SupplierPaymentInvoiceValue[] Value { get; set; }
    }
}
