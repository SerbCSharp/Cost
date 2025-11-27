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
        public DateTime? Date { get; set; }

        [JsonPropertyName("Сумма")]
        public decimal? Sum { get; set; }

        [JsonPropertyName("СуммаВключаетНДС")]
        public bool? AmountIncludesNDS { get; set; }

        [JsonPropertyName("СуммаНДС")]
        public decimal? SumNDS { get; set; }

        [JsonPropertyName("СтавкаНДС")]
        public string RateNDS { get; set; }

        [JsonPropertyName("ДоговорЗакрыт")]
        public bool? ContractClosed { get; set; }

        [JsonPropertyName("ИмпСуммаГарантийногоУдержания")]
        public decimal? AmountSecurityRetention { get; set; }

        [JsonPropertyName("ДополнительныеРеквизиты")]
        public AdditionalDetails[] AdditionalDetails { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }

        [JsonPropertyName("ДоговорПодписан")]
        public bool? AgreementSigned { get; set; }

        [JsonPropertyName("Owner_Key")]
        public string ContractorId { get; set; } // Подрядчик

        [JsonPropertyName("Организация_Key")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("ВидВзаиморасчетов_Key")]
        public string TypeCalculationId { get; set; }
        public string CostItemsId { get; set; }
        public string NomenclatureGroupsId { get; set; }
        public string CostItems { get; set; }
        public string ConstructionProjects { get; set; }
        public string TypeCalculation { get; set; }

        [JsonPropertyName("ВидДоговора")]
        public string TypeAgreement { get; set; }
        public bool? DeletionMark { get; set; }
        public string Contractor { get; set; }


    }
}
