using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptProcessing
{
    public class ReceiptProcessing
    {
        [JsonPropertyName("value")]
        public ReceiptProcessingValue[] Value { get; set; }
    }
}
