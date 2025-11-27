namespace Cost.Infrastructure.Repositories.Models
{
    public class Operations
    {
        public string OperationId { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public decimal Sum { get; set; }
        public string ContractDebit { get; set; }
        public string ContractCredit { get; set; }
        public string ContractorDebit { get; set; }
        public string ContractorCredit { get; set; }
    }
}
