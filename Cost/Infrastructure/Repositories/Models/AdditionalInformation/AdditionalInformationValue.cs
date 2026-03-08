using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.AdditionalInformation
{
    public class AdditionalInformationValue
    {
        [JsonPropertyName("Объект")]
        public string ADObject { get; set; }

        [JsonPropertyName("Значение")]
        public string ADValue { get; set; }

        [JsonPropertyName("Значение_Type")]
        public string ValueType { get; set; }
    }
}
