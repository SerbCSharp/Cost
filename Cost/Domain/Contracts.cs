using System.Text.Json.Serialization;

namespace Cost.Domain
{
    public class Contracts : IEquatable<Contracts>
    {
        public string ContractId { get; set; }
        public string Number { get; set; }
        public string NumberAA { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Sum { get; set; }
        public string Contractor { get; set; }
        public string Name { get; set; }
        public string ConstructionObject { get; set; }
        public string CostItem { get; set; }
        public string ContractorOrSupplier { get; set; }
        public string AmountIncludesNDS { get; set; }
        public decimal RateNDS { get; set; }
        public string ContractClosed { get; set; }

        [JsonPropertyName("ВидДоговора")]
        public string TypeAgreement { get; set; }
        public decimal GeneralContracting { get; set; }
        public decimal WarrantyLien { get; set; }

        public bool Equals(Contracts other)
        {
            if (other is null)
                return false;

            return this.ContractId == other.ContractId;
        }

        public override bool Equals(object obj) => Equals(obj as Contracts);
        public override int GetHashCode() => ContractId.GetHashCode();
    }
}
