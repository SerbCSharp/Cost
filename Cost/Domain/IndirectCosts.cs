namespace Cost.Domain
{
    public class IndirectCosts
    {
        public int Number { get; set; }
        public string PaymentId { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
        public decimal Sum { get; set; }
    }
}