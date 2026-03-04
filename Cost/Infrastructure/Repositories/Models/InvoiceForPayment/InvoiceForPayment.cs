using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.InvoiceForPayment
{
    public class InvoiceForPayment
    {
        [JsonPropertyName("value")]
        public InvoiceForPayment[] Value { get; set; }
    }
}
