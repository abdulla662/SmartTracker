using DealTrack.Domain.Common;
using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Entities
{
    public class Target : BaseEntity
    {
        public Guid TenantId { get; private set; }
        public string AssignedToUserId { get; private set; }
        public string CreatedByUserId { get; private set; }
        public TargetType TargetType { get; private set; }
        public string? CustomTypeName { get; private set; }
        public string? CustomTypeUnit { get; private set; }
        public decimal? Value { get; private set; }
        public int Month { get; private set; }
        public int Year { get; private set; }

        private Target() { AssignedToUserId = ""; CreatedByUserId = ""; }

        public Target(Guid tenantId, string assignedToUserId, string createdByUserId, TargetType targetType, decimal? value, int month, int year, string? customTypeName = null, string? customTypeUnit = null)
        {
            TenantId = tenantId;
            AssignedToUserId = assignedToUserId;
            CreatedByUserId = createdByUserId;
            TargetType = targetType;
            Value = value;
            Month = month;
            Year = year;
            CustomTypeName = customTypeName;
            CustomTypeUnit = customTypeUnit;
        }

        public void Update(decimal? value, string? customTypeName = null, string? customTypeUnit = null)
        {
            Value = value;
            if (customTypeName != null) CustomTypeName = customTypeName;
            if (customTypeUnit != null) CustomTypeUnit = customTypeUnit;
            MarkUpdated();
        }
    }
}
