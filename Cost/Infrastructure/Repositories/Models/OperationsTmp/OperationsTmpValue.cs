using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.OperationsTmp
{
    public class OperationsTmpValue
    {
        public string Ref_Key { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }

        [JsonPropertyName("СуммаОперации")]
        public decimal Sum { get; set; }
        public bool DeletionMark { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("Содержание")]
        public string Content { get; set; }

        [JsonPropertyName("СпособЗаполнения")]
        public string FillingMethod { get; set; }
    }
}
