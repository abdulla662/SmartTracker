namespace DealTrack.Application.Common
{
    public class RequestFilterContext
    {
        public Guid? UserId { get; set; }
        public long? OrganizationId { get; set; }
        public bool ShowDeleted { get; set; }
        public bool IsActive { get; set; } = true;
        public bool ApplyOrganizationFilter { get; set; } = true;
        public bool ApplySoftDeleteFilter { get; set; } = true;
        public string? Signature { get; set; }
        public IReadOnlyList<string>? Roles { get; set; }
    }
}
