using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DepositToCurrentAccount
{
    public class DepositToCurrentAccount
    {
        [JsonPropertyName("value")]
        public DepositToCurrentAccountValue[] Value { get; set; }
    }
}
