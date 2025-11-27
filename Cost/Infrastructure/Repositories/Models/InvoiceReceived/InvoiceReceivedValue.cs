using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.InvoiceReceived
{
    public class InvoiceReceivedValue
    {
        [JsonPropertyName("Ref_Key")]
        public string InvoiceReceivedId { get; set; }
        public bool Posted { get; set; }
        public bool DeletionMark { get; set; }

        [JsonPropertyName("ПредставлениеНомера")]
        public string Number { get; set; }
        public DateTime Date { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string CounterpartyAgreementId { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("СуммаНДСДокумента")]
        public decimal DocumentNDSAmount { get; set; }

        [JsonPropertyName("ВидСчетаФактуры")]
        public string TypeInvoice { get; set; }

        [JsonPropertyName("ДокументОснование")]
        public string DocumentId { get; set; }

        [JsonPropertyName("ДокументОснование_Type")]
        public string DocumentType { get; set; }

        [JsonPropertyName("СуммаУменьшение")]
        public decimal AmountMinus { get; set; }

        [JsonPropertyName("СуммаУвеличение")]
        public decimal AmountPlus { get; set; }

        [JsonPropertyName("СуммаНДСУменьшение")]
        public decimal AmountNDSMinus { get; set; }

        [JsonPropertyName("СуммаНДСУвеличение")]
        public decimal AmountNDSPlus { get; set; }
    }
}
