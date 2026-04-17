namespace Cost.Domain
{
    public class IndirectCosts
    {
        public DateOnly Date { get; set; }
        public string PaymentId { get; set; }
        public decimal Ketov { get; set; }
        public decimal Gontar { get; set; }
        public decimal Endulsi { get; set; }
        public decimal TechnicalCustomer { get; set; }
        public decimal TransportRental { get; set; }
        public decimal Withdrawal { get; set; }
        public bool DirectOrIndirect { get; set; }
        public decimal SalesDepartment { get; set; }
        public decimal Rent { get; set; }
    }
}
