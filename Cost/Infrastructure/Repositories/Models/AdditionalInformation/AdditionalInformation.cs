using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.AdditionalInformation
{
    public class AdditionalInformation
    {
        [JsonPropertyName("value")]
        public AdditionalInformationValue[] Value { get; set; }
    }
}
