namespace Cost.Domain
{
    public class ExpensesUnderIncomeContracts
    {
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public DateOnly Date { get; set; }
        public decimal Sum { get; set; }
        public string Liter { get; set; }
        public decimal Receipt { get; set; }
        public decimal Payment { get; set; }
        public decimal Expenses { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
    }
}
