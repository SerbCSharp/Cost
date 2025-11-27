using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Counterparties
{
    public class Counterparties
    {
        [JsonPropertyName("value")]
        public CounterpartiesValue[] Value { get; set; }
    }
}
