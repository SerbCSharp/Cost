using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.UnitsOfMeasurement
{
    public class UnitsOfMeasurement
    {
        [JsonPropertyName("value")]
        public UnitsOfMeasurementValue[] Value { get; set; }
    }
}
