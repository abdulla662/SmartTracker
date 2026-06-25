using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ClientService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<ApiResponseT<PagedResult<ClientResponseDto>>> GetClientsAsync(
            ClientFilterDto filter, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.Role == "Admin";

            var all = await _uow.Read<Client>().ListAsync(c =>
                (isAdmin || c.AssignedToUserId == userId) &&
                (string.IsNullOrEmpty(filter.Search) ||
                 c.Name.Contains(filter.Search) ||
                 c.Phone.Contains(filter.Search)),
                ct);

            var totalCount = all.Count;
            var items = all
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(MapToDto)
                .ToList();

            return ApiResponseT<PagedResult<ClientResponseDto>>.SuccessResponse(new PagedResult<ClientResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponseT<ClientResponseDto>> GetClientByIdAsync(
            Guid id, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponseT<ClientResponseDto>.FailureResponse(
                    _localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponseT<ClientResponseDto>.FailureResponse(
                    _localizer["AccessDenied"], HttpStatusCode.Forbidden);

            return ApiResponseT<ClientResponseDto>.SuccessResponse(MapToDto(client));
        }

        public async Task<ApiResponseT<ClientResponseDto>> CreateClientAsync(
            CreateClientDto dto, CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;
            var userId = _currentUser.UserId;

            var phoneExists = await _uow.Read<Client>().AnyAsync(
                c => c.Phone == dto.Phone && c.AssignedToUserId == userId, ct);

            if (phoneExists)
                return ApiResponseT<ClientResponseDto>.FailureResponse(_localizer["ClientPhoneExists"]);

            var client = new Client(tenantId, dto.Name, dto.Phone, dto.Notes, userId);
            await _uow.Write<Client>().AddAsync(client, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<ClientResponseDto>.SuccessResponse(
                MapToDto(client), _localizer["ClientCreated"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> UpdateClientAsync(
            Guid id, UpdateClientDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            client.Update(dto.Name, dto.Phone, dto.Notes);
            await _uow.Write<Client>().UpdateAsync(client, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["ClientUpdated"]);
        }

        public async Task<ApiResponse> DeleteClientAsync(Guid id, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            await _uow.SoftDelete<Client>().SoftDeleteAsync(client, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["ClientDeleted"]);
        }

        private bool CanAccess(Client client) =>
            _currentUser.Role == "Admin" || client.AssignedToUserId == _currentUser.UserId;

        private static ClientResponseDto MapToDto(Client c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Notes = c.Notes,
            AssignedToUserId = c.AssignedToUserId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
