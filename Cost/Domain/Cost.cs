namespace Cost.Domain
{
    public class Cost
    {
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string Number { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Sum { get; set; }
        public string ConstructionObject { get; set; }
        public string CostItem { get; set; }
        public string ContractorOrSupplier { get; set; }
        public decimal TotalArea { get; set; }
        public decimal Receipt { get; set; }
        public decimal Payment { get; set; }
        public decimal Selling { get; set; }
        public decimal Payable { get; set; } // К оплате
        public decimal Receivable { get; set; } // Дебиторская задолженность (подлежащий получению)
        public decimal ReceiptToCurrentAccount { get; set; }
        public string ContractClosed { get; set; }
        public string AmountIncludesNDS { get; set; }
        public decimal RateNDS { get; set; }
        public decimal GeneralContracting { get; set; }
        public decimal WarrantyLien { get; set; }
        public string NumberAA { get; set; }
        public string Name { get; set; }
        public decimal SumDebit { get; set; }
        public decimal SumCredit { get; set; }
        public decimal ConstructionCost { get; set; }
        public decimal DocumentAmount { get; set; }
        public decimal DocumentNDSAmount { get; set; }
    }
}
