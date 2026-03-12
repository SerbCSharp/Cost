using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptGoodsServices
{
    public class ReceiptGoodsServices
    {
        [JsonPropertyName("value")]
        public ReceiptGoodsServicesValue[] Value { get; set; }
    }
}
