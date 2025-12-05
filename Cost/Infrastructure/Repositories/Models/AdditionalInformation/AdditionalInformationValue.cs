using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.AdditionalInformation
{
    public class AdditionalInformationValue
    {
        [JsonPropertyName("Объект")]
        public string Indicator { get; set; }

        [JsonPropertyName("Значение")]
        public string IndicatorValue { get; set; }

        [JsonPropertyName("Объект_Type")]
        public string IndicatorType { get; set; }

        [JsonPropertyName("Значение_Type")]
        public string ValueType { get; set; }
    }
}
