using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ContractsCounterparties
{
    public class ContractsCounterparties
    {
        [JsonPropertyName("value")]
        public ContractsCounterpartiesValue[] Value { get; set; }
    }
}
