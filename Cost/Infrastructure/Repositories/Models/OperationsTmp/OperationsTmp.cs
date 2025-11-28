using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.OperationsTmp
{
    public class OperationsTmp
    {
        [JsonPropertyName("value")]
        public OperationsTmpValue[] Value { get; set; }
    }
}
