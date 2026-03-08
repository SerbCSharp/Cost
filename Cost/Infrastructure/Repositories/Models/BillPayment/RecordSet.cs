using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.BillPayment
{
    public class RecordSet
    {
        [JsonPropertyName("СчетНаОплату")]
        public string InvoiceForPaymentId { get; set; }
    }
}
