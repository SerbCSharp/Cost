using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.SupplierPaymentInvoice
{
    public class SupplierPaymentInvoiceValue
    {
        [JsonPropertyName("Ref_Key")]
        public string SupplierPaymentInvoiceId { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }
    }
}
