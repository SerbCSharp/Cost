using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.TypesCalculations
{
    public class TypesCalculations
    {
        [JsonPropertyName("value")]
        public TypesCalculationsValue[] Value { get; set; }
    }
}
