namespace Cost.Domain
{
    public class AccountingTransaction
    {
        public DateOnly Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string ContractId { get; set; }
    }
}
