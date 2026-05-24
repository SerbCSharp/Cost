namespace Cost.Infrastructure.Repositories.Models
{
    public class AreaOfActivityPaymentsFromExcel : IEquatable<AreaOfActivityPaymentsFromExcel>
    {
        public string PaymentId { get; set; }
        public decimal Percent { get; set; }
        public string TypeOfActivity { get; set; }
        public string AreaOfActivity { get; set; }
        public bool DirectOrIndirect { get; set; }
        public string ContractIdIncome { get; set; }

        public bool Equals(AreaOfActivityPaymentsFromExcel other)
        {
            if (other is null)
                return false;

            return PaymentId == other.PaymentId;
        }

        public override bool Equals(object obj) => Equals(obj as AreaOfActivityPaymentsFromExcel);
        public override int GetHashCode() => PaymentId.GetHashCode();
    }
}
