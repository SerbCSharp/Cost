using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Receipts
{
    public class Receipts
    {
        [JsonPropertyName("value")]
        public ReceiptsValue[] Value { get; set; }
    }
}
