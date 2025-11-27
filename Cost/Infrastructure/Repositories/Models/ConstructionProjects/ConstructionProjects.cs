using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ConstructionProjects
{
    public class ConstructionProjects
    {
        [JsonPropertyName("value")]
        public ConstructionProjectsValue[] Value { get; set; }
    }
}
