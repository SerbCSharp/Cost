using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Payments
{
    public class Payments
    {
        [JsonPropertyName("value")]
        public PaymentsValue[] Value { get; set; }
    }
}
