namespace Cost.Domain
{
    public class HowMuchIsLeftToPayExtra
    {
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string Contract { get; set; }
        public string Contractor { get; set; }
        public decimal SupplierPaymentInvoiceAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentId { get; set; }

    }
}
