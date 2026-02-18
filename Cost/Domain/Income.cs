namespace Cost.Domain
{
    public class Income
    {
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Sum { get; set; }
        public string ConstructionObject { get; set; }
        public decimal Receipt { get; set; }
        public decimal Payment { get; set; }
        public string NumberAA { get; set; }
        public string Name { get; set; }
        public decimal AmountUntil2026 { get; set; }
        public decimal OutgoingNDS { get; set; }
    }
}
