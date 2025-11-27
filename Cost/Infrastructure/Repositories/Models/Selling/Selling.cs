using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Selling
{
    public class Selling
    {
        [JsonPropertyName("value")]
        public SellingValue[] Value { get; set; }
    }
}
