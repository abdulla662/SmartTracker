using DealTrack.Application.Common;
using DealTrack.Application.DTOs.FollowUps;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public FollowUpService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<ApiResponseT<List<FollowUpResponseDto>>> GetFollowUpsForClientAsync(
            Guid clientId, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponseT<List<FollowUpResponseDto>>.FailureResponse(
                    _localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var followUps = await _uow.Read<FollowUp>().ListAsync(f => f.ClientId == clientId, ct);

            var dtos = followUps
                .OrderBy(f => f.FollowUpDate)
                .Select(f => MapToDto(f, client.Name))
                .ToList();

            return ApiResponseT<List<FollowUpResponseDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<FollowUpResponseDto>> CreateFollowUpAsync(
            CreateFollowUpDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(dto.ClientId, ct);
            if (client is null)
                return ApiResponseT<FollowUpResponseDto>.FailureResponse(
                    _localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var followUp = new FollowUp(
                _currentUser.TenantId,
                dto.ClientId,
                dto.FollowUpDate,
                Guid.Parse(_currentUser.UserId),
                dto.Notes);

            await _uow.Write<FollowUp>().AddAsync(followUp, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<FollowUpResponseDto>.SuccessResponse(
                MapToDto(followUp, client.Name), _localizer["FollowUpCreated"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> MarkDoneAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            followUp.MarkDone();
            await _uow.Write<FollowUp>().UpdateAsync(followUp, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpMarkedDone"]);
        }

        public async Task<ApiResponse> MarkMissedAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            followUp.MarkMissed();
            await _uow.Write<FollowUp>().UpdateAsync(followUp, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpMarkedMissed"]);
        }

        public async Task<ApiResponse> DeleteFollowUpAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            await _uow.SoftDelete<FollowUp>().SoftDeleteAsync(followUp, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpDeleted"]);
        }

        private static FollowUpResponseDto MapToDto(FollowUp f, string clientName) => new()
        {
            Id = f.Id,
            ClientId = f.ClientId,
            ClientName = clientName,
            FollowUpDate = f.FollowUpDate,
            Status = f.Status,
            Notes = f.Notes,
            CreatedByUserId = f.CreatedByUserId,
            CreatedAt = f.CreatedAt
        };
    }
}
