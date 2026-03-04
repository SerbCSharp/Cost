using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ActOfCompletion
{
    public class ActOfCompletion
    {
        [JsonPropertyName("value")]
        public ActOfCompletionValue[] Value { get; set; }
    }
}
