namespace Cost.Domain
{
    public class ReconciliationStatement
    {
        public string Contractor { get; set; }
        public string Name { get; set; }
        public decimal? Sum { get; set; }
        public DateTime Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string DocumentName { get; set; }
    }
}
