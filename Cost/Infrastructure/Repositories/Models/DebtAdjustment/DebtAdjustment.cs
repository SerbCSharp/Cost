using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebtAdjustment
{
    public class DebtAdjustment
    {
        [JsonPropertyName("value")]
        public DebtAdjustmentValue[] Value { get; set; }
    }
}
