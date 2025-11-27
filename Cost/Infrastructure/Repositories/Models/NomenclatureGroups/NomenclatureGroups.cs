using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.NomenclatureGroups
{
    public class NomenclatureGroups
    {
        [JsonPropertyName("value")]
        public NomenclatureGroupsValue[] Value { get; set; }
    }
}
