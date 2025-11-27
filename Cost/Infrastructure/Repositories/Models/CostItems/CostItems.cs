using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.CostItems
{
    public class CostItems
    {
        [JsonPropertyName("value")]
        public CostItemsValue[] Value { get; set; }
    }
}
