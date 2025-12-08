namespace Cost.Infrastructure.Repositories.Models
{
    public class LiterAndCostItemInPayments
    {
        public string PaymentId { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public decimal PaymentAmount { get; set; }
        public string Liter { get; set; }
        public string CostItems { get; set; }
        public string PurposePayment { get; set; }
        public string CostItemsInAgreement { get; set; }
        public string Contractor { get; set; }
        public decimal PaymentNDSAmount { get; set; }
        public string LiterInAgreement { get; set; }
        public string ContractorOrSupplier { get; set; }
        public string ContractId { get; set; }
    }
}
