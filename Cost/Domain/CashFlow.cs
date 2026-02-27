namespace Cost.Domain
{
    public class CashFlow
    {
        public string AreaOfActivity { get; set; }
        public decimal Payment { get; set; }
        public decimal Receipt { get; set; }
        public string Organization { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal StartBalance { get; set; }
    }
}
