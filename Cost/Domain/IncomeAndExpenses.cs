namespace Cost.Domain
{
    public class IncomeAndExpenses
    {
        public DateTime Date { get; set; }
        public decimal Payment { get; set; }
        public decimal Receipt { get; set; }
        public string DocumentName { get; set; }
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public decimal RateNDS { get; set; }
        public decimal GeneralContracting { get; set; }
        public decimal? DocumentAmount { get; set; }
        public decimal? DocumentNDSAmount { get; set; }
        public decimal? InvoiceReceivedNDS { get; set; }
        public DateTime? DateContract { get; set; }
        public decimal? SumContract { get; set; }
        public string ContractClosed { get; set; }
        public decimal WarrantyLien { get; set; }
        public string ConstructionObject { get; set; }
        public string CostItem { get; set; }
        public string ContractorOrSupplier { get; set; }
        public string LiterPayment { get; set; }
        public string CostItemPayment { get; set; }
        public string Name { get; set; }
    }
}
