using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ContractsCounterparties
{
    public class AdditionalDetails
    {
        [JsonPropertyName("Значение")]
        public string Value { get; set; }

        [JsonPropertyName("Значение_Type")]
        public string ValueType { get; set; }
    }
}
