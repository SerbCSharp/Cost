using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebtAdjustment
{
    public class AccountsReceivable
    {
        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }

        [JsonPropertyName("КорДоговорКонтрагента_Key")]
        public string CorCounterpartyAgreementId { get; set; }

        [JsonPropertyName("Сумма")]
        public decimal Sum { get; set; }
    }
}
