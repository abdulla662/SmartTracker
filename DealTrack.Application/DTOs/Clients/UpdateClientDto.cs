using System.ComponentModel.DataAnnotations;

namespace DealTrack.Application.DTOs.Clients
{
    public class UpdateClientDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
