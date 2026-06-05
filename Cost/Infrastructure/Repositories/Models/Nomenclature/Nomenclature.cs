using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.Nomenclature
{
    public class Nomenclature
    {
        [JsonPropertyName("value")]
        public NomenclatureValue[] Value { get; set; }
    }
}
