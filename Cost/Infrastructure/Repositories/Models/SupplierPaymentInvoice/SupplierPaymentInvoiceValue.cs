using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.SupplierPaymentInvoice
{
    public class SupplierPaymentInvoiceValue
    {
        [JsonPropertyName("Ref_Key")]
        public string SupplierPaymentInvoiceId { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal PaymentAmount { get; set; }
        public DateTime Date { get; set; }
        public string Number { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("Контрагент_Key")]
        public string ContractorId { get; set; }
    }
}
