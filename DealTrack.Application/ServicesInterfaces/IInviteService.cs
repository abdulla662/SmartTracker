using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TenantInvite;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IInviteService
    {
        public Task<ApiResponseT<GetInviteDto>> CreateInviteAsync(CreateInviteDto dto, CancellationToken ct = default);
        public Task<ApiResponseT<AcceptInviteDto>> AcceptInviteAsync(AcceptInviteDto dto, CancellationToken ct = default);
        public Task<ApiResponseT<List<GetInviteDto>>> GetAllInvites(CancellationToken ct = default);
        public Task<ApiResponseT<InviteInfoDto>> GetInviteInfoAsync(string code, CancellationToken ct = default);
        public Task<ApiResponseT<GetInviteDto>> RevokeAndResendAsync(string email, CancellationToken ct = default);
    }
}
