using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebitToCurrentAccount
{
    public class DebitToCurrentAccount
    {
        [JsonPropertyName("value")]
        public DebitToCurrentAccountValue[] Value { get; set; }
    }
}
