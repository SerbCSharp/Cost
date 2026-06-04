using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ReceiptGoodsServices
{
    public class Good
    {
        [JsonPropertyName("Номенклатура_Key")]
        public string NomenclatureId { get; set; }

        [JsonPropertyName("ЕдиницаИзмерения_Key")]
        public string UnitsOfMeasurementId { get; set; }

        [JsonPropertyName("Количество")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("Цена")]
        public decimal Price { get; set; }

        [JsonPropertyName("Сумма")]
        public decimal Sum { get; set; }

        [JsonPropertyName("СуммаНДС")]
        public decimal SumNDS { get; set; }
    }
}
