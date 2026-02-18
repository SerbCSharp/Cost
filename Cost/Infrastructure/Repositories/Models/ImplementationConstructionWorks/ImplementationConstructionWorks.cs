using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ImplementationConstructionWorks
{
    public class ImplementationConstructionWorks
    {
        [JsonPropertyName("value")]
        public ImplementationConstructionWorksValue[] Value { get; set; }
    }
}
