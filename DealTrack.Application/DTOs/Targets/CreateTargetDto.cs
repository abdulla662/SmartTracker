using DealTrack.Domain.Enums;

namespace DealTrack.Application.DTOs.Targets
{
    public class CreateTargetDto
    {
        public string? AssignedToUserId { get; set; }
        public TargetType TargetType { get; set; }
        public string? CustomTypeName { get; set; }
        public string? CustomTypeUnit { get; set; }
        public decimal? Value { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
