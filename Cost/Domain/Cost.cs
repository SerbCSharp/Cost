namespace Cost.Domain
{
    public class Cost
    {
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public DateOnly Date { get; set; }
        public decimal? Sum { get; set; }
        public string ConstructionObject { get; set; }
        public string CostItem { get; set; }
        public string ContractorOrSupplier { get; set; }
        public decimal TotalArea { get; set; }
        public decimal Receipt { get; set; }
        public decimal Payment { get; set; }
        public string ContractClosed { get; set; }
        public decimal RateNDS { get; set; }
        public decimal GeneralContracting { get; set; }
        public decimal WarrantyLien { get; set; }
        public string NumberAA { get; set; }
        public string Name { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal AmountUntil2026 { get; set; }
        public decimal RateNDS2026 { get; set; }
        public int Year { get; set; }
        public decimal ConstructionCostNDS { get; set; }
        public decimal InputNDS { get; set; }
        public decimal Expenses { get; set; }
        public string ResidentialComplex { get; set; }
        public decimal CurrentDebt { get; set; }
    }
}
