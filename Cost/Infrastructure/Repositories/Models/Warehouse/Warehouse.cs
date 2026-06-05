using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Warehouse
{
    public class Warehouse
    {
        [JsonPropertyName("value")]
        public WarehouseValue[] Value { get; set; }
    }
}
