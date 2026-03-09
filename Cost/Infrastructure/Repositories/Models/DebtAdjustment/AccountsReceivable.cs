using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebtAdjustment
{
    public class AccountsReceivable
    {
        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("КорДоговорКонтрагента_Key")]
        public string CorContractId { get; set; }

        [JsonPropertyName("Сумма")]
        public decimal Sum { get; set; }
    }
}
