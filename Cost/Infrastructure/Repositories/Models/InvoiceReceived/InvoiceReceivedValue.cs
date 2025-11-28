using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.InvoiceReceived
{
    public class InvoiceReceivedValue
    {
        public bool Posted { get; set; }
        public bool DeletionMark { get; set; }

        [JsonPropertyName("СуммаДокумента")]
        public decimal DocumentAmount { get; set; }

        [JsonPropertyName("СуммаНДСДокумента")]
        public decimal DocumentNDSAmount { get; set; }

        [JsonPropertyName("ДокументОснование")]
        public string DocumentId { get; set; }

        [JsonPropertyName("ДокументОснование_Type")]
        public string DocumentType { get; set; }
    }
}
