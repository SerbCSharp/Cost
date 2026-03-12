using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.SaleGoodsServices
{
    public class SaleGoodsServices
    {
        [JsonPropertyName("value")]
        public SaleGoodsServicesValue[] Value { get; set; }
    }
}
