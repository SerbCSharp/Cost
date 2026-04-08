namespace Cost.Infrastructure.Repositories.Models
{
    public class ExpensePaymentsFromExcel
    {
        public string PaymentId { get; set; }
        public DateOnly Date { get; set; }
        public decimal PaymentAmount { get; set; }
        public string Liter { get; set; }
        public string CostItems { get; set; }
        public string PurposePayment { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }

        public bool Equals(ExpensePaymentsFromExcel other)
        {
            if (other is null)
                return false;

            return PaymentId == other.PaymentId;
        }

        public override bool Equals(object obj) => Equals(obj as ExpensePaymentsFromExcel);
        public override int GetHashCode() => PaymentId.GetHashCode();
    }
}
