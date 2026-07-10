namespace DealTrack.Application.DTOs.Payments
{
    public class ClientPaymentSummaryDto
    {
        public Guid   ClientId        { get; set; }
        public string ClientName      { get; set; } = string.Empty;
        public string AssignedToName  { get; set; } = string.Empty;
        public decimal TotalAmount    { get; set; }
        public decimal PaidAmount     { get; set; }
        public decimal Remaining      { get; set; }
        public string  Status         { get; set; } = string.Empty;
        public int     PaymentCount   { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    public class SetDealAmountDto
    {
        public decimal TotalDealAmount { get; set; }
    }
}
