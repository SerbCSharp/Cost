using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ContractsCounterparties
{
    public class ContractsCounterpartiesValue
    {
        [JsonPropertyName("Ref_Key")]
        public string CounterpartyAgreementId { get; set; }

        [JsonPropertyName("Номер")]
        public string Number { get; set; }

        [JsonPropertyName("Description")]
        public string Name { get; set; }

        [JsonPropertyName("Дата")]
        public DateTime Date { get; set; }

        [JsonPropertyName("Сумма")]
        public decimal Sum { get; set; }

        [JsonPropertyName("Owner_Key")]
        public string ContractorId { get; set; } // Подрядчик
        public string Code { get; set; }
    }
}
