using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.ActOfCompletion
{
    public class ActOfCompletionValue
    {
        public bool DeletionMark { get; set; }
        public bool Posted { get; set; }

        [JsonPropertyName("ДоговорКонтрагента_Key")]
        public string ContractId { get; set; }

        [JsonPropertyName("ДатаНачала")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("ДатаОкончания")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("Комментарий")]
        public string Comment { get; set; }
    }
}
