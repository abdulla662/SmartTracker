using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TransferRequest;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;


namespace DealTrack.Application.Services
{
    public class TransferRequestService : ITransferRequestService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly INotificationService _notifications;

        public TransferRequestService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, INotificationService notifications)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _notifications = notifications;
        }

        public async Task<ApiResponseT<TransferRequestResponseDto>> CreateTransferRequestAsync(CreateTransferRequestDto dto, CancellationToken ct)
        {
            // بس TeamLead يقدر يعمل Transfer Request
            if (_currentUser.Role != UserRole.TeamLead)
                return ApiResponseT<TransferRequestResponseDto>.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            // تأكد إن الـ Sales موجود وتحت هذا الـ TeamLead
            var sales = await _uow.Read<ApplicationUser>()
                .GetSingleAsync(u => u.Id == dto.SalesUserId && u.TeamLeadId == Guid.Parse(_currentUser.UserId), ct);
            if (sales is null)
                return ApiResponseT<TransferRequestResponseDto>.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            // تأكد إن الـ ToTeamLead موجود في نفس الـ Tenant
            var toTeamLead = await _uow.Read<ApplicationUser>()
                .GetSingleAsync(u => u.Id == dto.ToTeamLeadId && u.TenantId == _currentUser.TenantId && u.Role == UserRole.TeamLead, ct);
            if (toTeamLead is null)
                return ApiResponseT<TransferRequestResponseDto>.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            // تأكد مفيش Request pending لنفس الـ Sales
            var existingRequest = await _uow.Read<TransferRequest>()
                .AnyAsync(r => r.SalesUserId == dto.SalesUserId && r.Status == TransferRequestStatus.Pending, ct);
            if (existingRequest)
                return ApiResponseT<TransferRequestResponseDto>.FailureResponse(_localizer["TransferRequestAlreadyPending"], HttpStatusCode.BadRequest);

            var request = new TransferRequest(_currentUser.TenantId, dto.SalesUserId, _currentUser.UserId, dto.ToTeamLeadId);
            await _uow.Write<TransferRequest>().AddAsync(request, ct);
            await _uow.SaveChangesAsync();

            var fromUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            await _notifications.CreateAsync(
                Guid.Parse(dto.ToTeamLeadId), _currentUser.TenantId,
                NotifKey.Build("notif.title.transferRequest"),
                NotifKey.Build("notif.msg.transferRequest", fromUser?.FullName ?? "", sales.FullName),
                NotificationType.TransferRequestReceived, ct);

            return ApiResponseT<TransferRequestResponseDto>.SuccessResponse(
                await MapToDto(request, ct), _localizer["TransferRequestCreated"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> RespondToTransferRequestAsync(Guid requestId, RespondTransferRequestDto dto, CancellationToken ct)
        {
            var request = await _uow.Read<TransferRequest>().GetByIdAsync(requestId, ct);
            if (request is null)
                return ApiResponse.FailureResponse(_localizer["TransferRequestNotFound"], HttpStatusCode.NotFound);

            // بس الـ ToTeamLead يقدر يرد — GUID casing can differ between JWT claim and DB
            if (!string.Equals(request.ToTeamLeadId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            if (request.Status != TransferRequestStatus.Pending)
                return ApiResponse.FailureResponse(_localizer["TransferRequestAlreadyHandled"], HttpStatusCode.BadRequest);

            if (dto.Accept)
            {
                request.Accept();

                // نقل الـ Sales للـ TeamLead الجديد
                var sales = await _uow.Read<ApplicationUser>()
                    .GetSingleAsync(u => u.Id == request.SalesUserId, ct);
                if (sales is not null)
                {
                    sales.TeamLeadId = Guid.Parse(request.ToTeamLeadId);
                    await _uow.Write<ApplicationUser>().UpdateAsync(sales, ct);
                }
            }
            else
            {
                request.Reject();
            }

            await _uow.Write<TransferRequest>().UpdateAsync(request, ct);
            await _uow.SaveChangesAsync();

            var respondingSales = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == request.SalesUserId, ct);
            var notifType = dto.Accept ? NotificationType.TransferRequestAccepted : NotificationType.TransferRequestRejected;
            var responderName = _currentUser.UserName ?? "";
            await _notifications.CreateAsync(Guid.Parse(request.FromTeamLeadId), _currentUser.TenantId,
                NotifKey.Build(dto.Accept ? "notif.title.transferAccepted" : "notif.title.transferRejected"),
                NotifKey.Build(dto.Accept ? "notif.msg.transferAccepted" : "notif.msg.transferRejected", responderName, respondingSales?.FullName ?? ""),
                notifType, ct);

            return ApiResponse.SuccessResponse(message: dto.Accept
                ? _localizer["TransferRequestAccepted"]
                : _localizer["TransferRequestRejected"]);
        }

        public async Task<ApiResponseT<List<TransferRequestResponseDto>>> GetMyTransferRequestsAsync(CancellationToken ct)
        {
            // TeamLead يشوف الـ Requests اللي بعتها + اللي جاتله
            var requests = await _uow.Read<TransferRequest>()
                .ListAsync(r => r.FromTeamLeadId == _currentUser.UserId || r.ToTeamLeadId == _currentUser.UserId, ct);

            var dtos = new List<TransferRequestResponseDto>();
            foreach (var r in requests)
                dtos.Add(await MapToDto(r, ct));

            return ApiResponseT<List<TransferRequestResponseDto>>.SuccessResponse(dtos);
        }

        private async Task<TransferRequestResponseDto> MapToDto(TransferRequest r, CancellationToken ct)
        {
            var sales = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == r.SalesUserId, ct);
            var fromTL = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == r.FromTeamLeadId, ct);
            var toTL = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == r.ToTeamLeadId, ct);

            return new TransferRequestResponseDto
            {
                Id = r.Id,
                SalesUserId = r.SalesUserId,
                SalesFullName = sales?.FullName ?? "",
                FromTeamLeadId = r.FromTeamLeadId,
                FromTeamLeadName = fromTL?.FullName ?? "",
                ToTeamLeadId = r.ToTeamLeadId,
                ToTeamLeadName = toTL?.FullName ?? "",
                Status = r.Status,
                CreatedAt = r.CreatedAt
            };
        }
    }
}