using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.InvoiceReceived
{
    public class InvoiceReceived
    {
        [JsonPropertyName("value")]
        public InvoiceReceivedValue[] Value { get; set; }
    }
}
