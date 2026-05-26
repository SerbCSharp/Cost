namespace Cost.Domain
{
    public class BreakdownOfExpensesUnderRevenueContracts
    {
        public DateOnly Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Expenses { get; set; }
        public string DocumentName { get; set; }
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public string Liter { get; set; }
        public string CostItem { get; set; }
        public string TypeOperation { get; set; }
        public string PaymentPurpose { get; set; }
        public string ContractIdIncome { get; set; }
        public string PaymentId { get; set; }
    }
}
