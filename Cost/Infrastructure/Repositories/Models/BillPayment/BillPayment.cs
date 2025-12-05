using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.BillPayment
{
    public class BillPayment
    {
        [JsonPropertyName("value")]
        public BillPaymentValue[] Value { get; set; }
    }
}
