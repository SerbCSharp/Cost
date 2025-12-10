using System.Text.Json.Serialization;

namespace Cost.Domain
{
    public class Nomenclature
    {
        public string Description { get; set; }

        [JsonPropertyName("ОбъектСтроительстваС_Key")]
        public string ConstructionObjectId { get; set; }
        public string ConstructionName { get; set; }
    }
}
