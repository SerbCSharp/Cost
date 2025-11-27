using System.Text.Json.Serialization;

namespace Cost.Infrastructure.Repositories.Models.DebtAdjustment
{
    public class DebtAdjustmentValue
    {
        public string Ref_Key { get; set; }
        public DateTime Date { get; set; }

        [JsonPropertyName("КредиторскаяЗадолженность")]
        public AccountsPayable[] AccountsPayable { get; set; }

        [JsonPropertyName("ДебиторскаяЗадолженность")]
        public AccountsReceivable[] AccountsReceivable { get; set; }
        public bool Posted { get; set; }
        public bool DeletionMark { get; set; }
    }
}
