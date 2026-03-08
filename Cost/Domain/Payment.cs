namespace Cost.Domain
{
    public class Payment
    {
        public string PaymentId { get; set; }
        public DateOnly Date { get; set; }
        public decimal PaymentAmount { get; set; }
        public string ContractId { get; set; }
        public string PaymentDetailsId { get; set; }
        public string Liter { get; set; }
        public string CostItem { get; set; }
        public string PaymentPurpose { get; set; }
        public string TypeOperation { get; set; }
        public string CommentFromPaymentInvoice { get; set; }
    }
}
