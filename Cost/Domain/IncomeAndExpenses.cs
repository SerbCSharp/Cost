namespace Cost.Domain
{
    public class IncomeAndExpenses
    {
        public DateOnly Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string DocumentName { get; set; }
        public string ContractId { get; set; }
        public string Liter { get; set; }
        public string CostItem { get; set; }
        public string TypeOperation { get; set; }
        public string PaymentPurpose { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
        public string PaymentId { get; set; }
    }
}
