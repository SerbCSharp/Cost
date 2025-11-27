using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.NomenclatureGroups
{
    public class NomenclatureGroupsValue
    {
        public string Ref_Key { get; set; }

        [JsonPropertyName("ОбъектСтроительстваС_Key")]
        public string ConstructionObjectId { get; set; }
    }
}