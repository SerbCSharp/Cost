namespace Cost.Domain
{
    public class CashFlow
    {
        public string PaymentId { get; set; }
        public string Number { get; set; }
        public DateOnly Date { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal Percent { get; set; }
        public string Liter { get; set; }
        public string CostItem { get; set; }
        public string PaymentPurpose { get; set; }
        public string Contractor { get; set; }
        public string TypeOperation { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
        public bool DirectOrIndirect { get; set; }
        public string ContractIdIncome { get; set; }
        public decimal SumTypeOfActivity { get; set; }
        public decimal IndirectCosts { get; set; }
        public decimal RateNDS { get; set; }
    }
}