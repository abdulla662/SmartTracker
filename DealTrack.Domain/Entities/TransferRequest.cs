using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class TransferRequest : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public string SalesUserId { get; private set; }
        public string FromTeamLeadId { get; private set; }
        public string ToTeamLeadId { get; private set; }
        public TransferRequestStatus Status { get; private set; }

        private TransferRequest()
        {
            SalesUserId = string.Empty;
            FromTeamLeadId = string.Empty;
            ToTeamLeadId = string.Empty;
        }

        public TransferRequest(Guid tenantId, string salesUserId, string fromTeamLeadId, string toTeamLeadId)
        {
            TenantId = tenantId;
            SalesUserId = salesUserId;
            FromTeamLeadId = fromTeamLeadId;
            ToTeamLeadId = toTeamLeadId;
            Status = TransferRequestStatus.Pending;
        }

        public void Accept()
        {
            Status = TransferRequestStatus.Accepted;
            MarkUpdated();
        }

        public void Reject()
        {
            Status = TransferRequestStatus.Rejected;
            MarkUpdated();
        }
    }
}