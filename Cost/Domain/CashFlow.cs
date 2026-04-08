namespace Cost.Domain
{
    public class CashFlow
    {
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
        public decimal Payment { get; set; }
        public decimal Receipt { get; set; }
        public string PaymentPurpose { get; set; }
        public DateOnly Date { get; set; }
        public string ContractId { get; set; }
        public string Liter { get; set; }
        public string CostItem { get; set; }
        public string TypeOperation { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public string PaymentId { get; set; }
        public decimal SumTypeOfActivity { get; set; }
    }
}
