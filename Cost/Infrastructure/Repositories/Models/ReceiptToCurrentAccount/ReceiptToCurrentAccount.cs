using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptToCurrentAccount
{
    public class ReceiptToCurrentAccount
    {
        [JsonPropertyName("value")]
        public ReceiptToCurrentAccountValue[] Value { get; set; }
    }
}
