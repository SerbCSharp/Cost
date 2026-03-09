namespace Cost.Domain
{
    public class AccountingTransaction
    {
        public DateOnly Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string DocumentName { get; set; }



        public string OperationId { get; set; }
        public string Number { get; set; }
        public decimal Sum { get; set; }
        public string ContractDebit { get; set; }
        public string ContractCredit { get; set; }


    }
}
