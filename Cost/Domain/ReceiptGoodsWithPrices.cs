namespace Cost.Domain
{
    public class ReceiptGoodsWithPrices
    {
        public DateOnly Date { get; set; }
        public decimal DocumentAmount { get; set; }
        public string ContractId { get; set; }
        public string Contractor { get; set; }
        public string NomenclatureId { get; set; }
        public string Nomenclature { get; set; }
        public decimal Quantity { get; set; }
        public string UnitsOfMeasurementId { get; set; }
        public string UnitsOfMeasurement { get; set; }
        public decimal Price { get; set; }
        public decimal Sum { get; set; }
        public decimal SumNDS { get; set; }
        public string WarehouseId { get; set; }
        public string Warehouse { get; set; }

    }
}
