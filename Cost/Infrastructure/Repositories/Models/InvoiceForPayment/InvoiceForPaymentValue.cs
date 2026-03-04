using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.InvoiceForPayment
{
    public class InvoiceForPaymentValue
    {
        [JsonPropertyName("Ref_Key")]
        public string InvoiceForPaymentId { get; set; }
        public DateTime Date { get; set; }
        public bool DeletionMark { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }
    }
}
