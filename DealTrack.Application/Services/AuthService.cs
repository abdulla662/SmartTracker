using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.DTOs.Auth;
using DealTrack.Application.DTOs.Auth.Forget_Password;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DealTrack.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notifications;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IUnitOfWork uow,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUser,
            IEmailService emailService,
            INotificationService notifications)
        {
            _userManager = userManager;
            _configuration = configuration;
            _uow = uow;
            _localizer = localizer;
            _currentUser = currentUser;
            _emailService = emailService;
            _notifications = notifications;
        }

        public async Task<ApiResponse> RegisterAsync(RegisterDto request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return ApiResponse.FailureResponse(_localizer["EmailAlreadyExists"]);

            if (!Enum.IsDefined(typeof(SubscriptionPlan), request.SubscriptionPlan))
                return ApiResponse.FailureResponse(_localizer["InvalidSubscriptionPlan"]);

            var isIndividual = string.IsNullOrWhiteSpace(request.CompanyName);
            var requestedRole = request.RequestedRole ?? UserRole.Sales;

            // Admin must always provide a company name (they are the founder)
            if (isIndividual && requestedRole == UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["CompanyNameRequired"]);

            // Individual user (Sales/TeamLead with no company name) — create personal workspace
            if (isIndividual)
            {
                var personalTenantName = $"{request.FullName.Trim()}'s Workspace";
                var personalTenant = new Tenant(personalTenantName, SubscriptionPlan.Advanced, isPersonal: true);
                await _uow.Write<Tenant>().AddAsync(personalTenant);

                var individualUser = new ApplicationUser
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    UserName = request.Email,
                    Role = requestedRole,
                    SubscriptionPlan = SubscriptionPlan.Advanced,
                    TenantId = personalTenant.Id,
                    IsApproved = true
                };

                var indResult = await _userManager.CreateAsync(individualUser, request.Password);
                if (!indResult.Succeeded)
                    return ApiResponse.FailureResponse(indResult.Errors.First().Description);

                await _uow.SaveChangesAsync();
                return ApiResponse.SuccessResponse(message: _localizer["RegisterSuccess"]);
            }

            // Company name provided — look it up directly in DB (no full-table scan)
            var normalizedInput = request.CompanyName.Replace(" ", "").ToLower();
            var existingTenant = await _uow.Read<Tenant>()
                .GetSingleAsync(t => t.Name.Replace(" ", "").ToLower() == normalizedInput, default);

            // Existing company — validate admin exists before accepting join request
            if (existingTenant != null)
            {
                var admin = _userManager.Users
                    .FirstOrDefault(u => u.TenantId == existingTenant.Id && u.Role == UserRole.Admin);

                // Company has no admin → reject the join request
                if (admin == null)
                    return ApiResponse.FailureResponse(_localizer["CompanyHasNoAdmin"]);

                var joinRole = requestedRole == UserRole.Admin ? UserRole.Sales : requestedRole;

                var pendingUser = new ApplicationUser
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    UserName = request.Email,
                    Role = joinRole,
                    SubscriptionPlan = existingTenant.Plan,
                    TenantId = existingTenant.Id,
                    IsApproved = false,
                    HasBeenWelcomed = false
                };

                var pendingResult = await _userManager.CreateAsync(pendingUser, request.Password);
                if (!pendingResult.Succeeded)
                    return ApiResponse.FailureResponse(pendingResult.Errors.First().Description);

                // Notify Admin
                await _notifications.CreateAsync(
                    Guid.Parse(admin.Id),
                    existingTenant.Id,
                    NotifKey.Build("notif.title.joinRequest"),
                    NotifKey.Build("notif.msg.joinRequest", new[] { request.FullName, joinRole.ToString() }, "/team"),
                    NotificationType.SystemNotification);

                // Notify all HR users in the same tenant
                var hrUsers = _userManager.Users
                    .Where(u => u.TenantId == existingTenant.Id && u.Role == UserRole.HR && u.IsApproved)
                    .ToList();
                foreach (var hr in hrUsers)
                    await _notifications.CreateAsync(
                        Guid.Parse(hr.Id),
                        existingTenant.Id,
                        NotifKey.Build("notif.title.joinRequest"),
                        NotifKey.Build("notif.msg.joinRequest", new[] { request.FullName, joinRole.ToString() }, "/hr"),
                        NotificationType.SystemNotification);

                return ApiResponse.SuccessResponse(message: _localizer["JoinRequestSent"]);
            }

            // Company name not found — only Admin can create a new company
            if (requestedRole != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["CompanyNotFound"]);

            // Always create with Free plan — payment webhook upgrades it after confirmed payment
            var tenant = new Tenant(request.CompanyName, SubscriptionPlan.Free);
            await _uow.Write<Tenant>().AddAsync(tenant);

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                Role = UserRole.Admin,
                SubscriptionPlan = SubscriptionPlan.Free,
                TenantId = tenant.Id,
                IsApproved = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description);

            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["RegisterSuccess"]);
        }

        public async Task<ApiResponseT<PendingRequestInfoDto>> GetPendingRequestAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponseT<PendingRequestInfoDto>.FailureResponse(_localizer["UserNotFound"], System.Net.HttpStatusCode.NotFound);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.IsApproved)
                return ApiResponseT<PendingRequestInfoDto>.FailureResponse(_localizer["UserNotFound"], System.Net.HttpStatusCode.NotFound);

            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == user.TenantId, default);

            // Return only what the UI needs to show the pending screen.
            // Never expose FullName or Role to unauthenticated callers — prevents user enumeration.
            return ApiResponseT<PendingRequestInfoDto>.SuccessResponse(new PendingRequestInfoDto
            {
                FullName = string.Empty,
                CompanyName = tenant?.IsPersonal == false ? tenant.Name : string.Empty,
                Role = string.Empty
            });
        }

        public async Task<ApiResponse> SendWithdrawalCodeAsync(SendWithdrawalCodeDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || user.IsApproved)
                // Respond the same way so we don't reveal whether the email is registered
                return ApiResponse.SuccessResponse(message: _localizer["WithdrawalCodeSent"]);

            // Invalidate any previous withdrawal OTPs for this user
            var prefix = "WITHDRAWAL:";
            var existing = await _uow.Read<PasswordResetToken>()
                .ListAsync(t => t.UserId == user.Id && !t.IsUsed, default);
            foreach (var old in existing.Where(t => t.Token.StartsWith(prefix)))
            {
                old.MarkUsed();
                await _uow.Write<PasswordResetToken>().UpdateAsync(old, default);
            }

            // Generate a 6-digit numeric OTP
            var code = Random.Shared.Next(100_000, 999_999).ToString();
            var tokenRecord = new PasswordResetToken(
                user.Id,
                prefix + code,
                DateTime.UtcNow.AddMinutes(15));

            await _uow.Write<PasswordResetToken>().AddAsync(tokenRecord, default);
            await _uow.SaveChangesAsync();

            await _emailService.SendWithdrawalVerificationAsync(user.Email!, code);

            return ApiResponse.SuccessResponse(message: _localizer["WithdrawalCodeSent"]);
        }

        public async Task<ApiResponse> CancelPendingRequestAsync(CancelPendingRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"], System.Net.HttpStatusCode.NotFound);

            if (user.IsApproved)
                return ApiResponse.FailureResponse(_localizer["NoPendingRequest"]);

            // Verify the OTP code
            var prefix = "WITHDRAWAL:";
            var tokenRecord = await _uow.Read<PasswordResetToken>()
                .GetSingleAsync(t => t.UserId == user.Id && t.Token == prefix + dto.Code, default);

            if (tokenRecord == null || !tokenRecord.IsValid)
                return ApiResponse.FailureResponse(_localizer["InvalidWithdrawalCode"], System.Net.HttpStatusCode.BadRequest);

            tokenRecord.MarkUsed();
            await _uow.Write<PasswordResetToken>().UpdateAsync(tokenRecord, default);

            // Find the admin before deleting the user
            var admin = _userManager.Users
                .FirstOrDefault(u => u.TenantId == user.TenantId && u.Role == UserRole.Admin);

            var fullName = user.FullName;
            var roleStr  = user.Role.ToString();
            var tenantId = user.TenantId;

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description);

            await _uow.SaveChangesAsync();

            if (admin != null)
                await _notifications.CreateAsync(
                    Guid.Parse(admin.Id),
                    tenantId,
                    NotifKey.Build("notif.title.joinWithdrawn"),
                    NotifKey.Build("notif.msg.joinWithdrawn", new[] { fullName, roleStr }, "/team"),
                    NotificationType.SystemNotification);

            return ApiResponse.SuccessResponse(message: _localizer["JoinRequestCancelled"]);
        }

        public async Task<ApiResponseT<CompanyCheckResult>> CheckCompanyAsync(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return ApiResponseT<CompanyCheckResult>.SuccessResponse(
                    new CompanyCheckResult { Exists = false, HasAdmin = false });

            var normalizedInput = companyName.Replace(" ", "").ToLower();
            var tenant = await _uow.Read<Tenant>()
                .GetSingleAsync(t => t.Name.Replace(" ", "").ToLower() == normalizedInput, default);

            if (tenant == null)
                return ApiResponseT<CompanyCheckResult>.SuccessResponse(
                    new CompanyCheckResult { Exists = false, HasAdmin = false });

            var admin = _userManager.Users
                .FirstOrDefault(u => u.TenantId == tenant.Id && u.Role == UserRole.Admin);

            return ApiResponseT<CompanyCheckResult>.SuccessResponse(
                new CompanyCheckResult { Exists = true, HasAdmin = admin != null });
        }

        public async Task<ApiResponseT<AuthResponseDto>> LoginAsync(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["InvalidCredentials"]);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["InvalidCredentials"]);

            // Individual user lock (set by SuperAdmin or Admin) takes precedence over all other checks
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["AccountSuspended"], HttpStatusCode.Forbidden);

            if (!user.IsApproved && user.Role != UserRole.Admin && user.Role != UserRole.SuperAdmin)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["AccountPendingApproval"], HttpStatusCode.Forbidden);

            if (user.Role != UserRole.SuperAdmin)
            {
                var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == user.TenantId, default);
                if (tenant != null && tenant.IsBlocked)
                {
                    var stillBlocked = tenant.BlockedUntil == null || tenant.BlockedUntil > DateTime.UtcNow;
                    if (stillBlocked)
                        return ApiResponseT<AuthResponseDto>.FailureResponse(
                            _localizer["AccountSuspended"], HttpStatusCode.Forbidden);
                }
            }

            var accessToken = GenerateJwtToken(user);

            var refreshExpireDays = Convert.ToDouble(_configuration["JwtSettings:RefreshExpireDays"] ?? "7");
            var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var refreshToken = new RefreshToken(
                user.TenantId,
                user.Id,
                HashToken(rawToken),
                DateTime.UtcNow.AddDays(refreshExpireDays));

            await _uow.Write<RefreshToken>().AddAsync(refreshToken);

            // Detect and clear first-login flag for newly accepted users
            bool isFirstLogin = !user.HasBeenWelcomed;
            if (isFirstLogin)
            {
                user.HasBeenWelcomed = true;
                await _userManager.UpdateAsync(user);
            }

            await _uow.SaveChangesAsync();

            return ApiResponseT<AuthResponseDto>.SuccessResponse(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = rawToken,   // send the raw token to client; DB stores the hash
                IsFirstLogin = isFirstLogin
            }, _localizer["LoginSuccess"]);
        }

        public async Task<ApiResponseT<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct)
        {
            var hashed = HashToken(dto.RefreshToken);
            var stored = await _uow.Read<RefreshToken>()
                .GetSingleAsync(r => r.Token == hashed, ct);

            if (stored is null || !stored.IsActive)
                return ApiResponseT<AuthResponseDto>.FailureResponse(
                    _localizer["InvalidRefreshToken"], HttpStatusCode.Unauthorized);

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user is null)
                return ApiResponseT<AuthResponseDto>.FailureResponse(
                    _localizer["UserNotFound"], HttpStatusCode.NotFound);

            stored.Revoke();
            await _uow.Write<RefreshToken>().UpdateAsync(stored, ct);

            var refreshExpireDays2 = Convert.ToDouble(_configuration["JwtSettings:RefreshExpireDays"] ?? "7");
            var newRawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var newRefreshToken = new RefreshToken(
                user.TenantId, user.Id,
                HashToken(newRawToken),
                DateTime.UtcNow.AddDays(refreshExpireDays2));

            await _uow.Write<RefreshToken>().AddAsync(newRefreshToken, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<AuthResponseDto>.SuccessResponse(new AuthResponseDto
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = newRawToken   // send raw; DB stores hash
            });
        }

        public async Task<ApiResponse> LogoutAsync(CancellationToken ct)
        {
            var userId = _currentUser.UserId;

            var tokens = await _uow.Read<RefreshToken>()
                .ListAsync(r => r.UserId == userId && !r.IsRevoked, ct);

            foreach (var token in tokens)
            {
                token.Revoke();
                await _uow.Write<RefreshToken>().UpdateAsync(token, ct);
            }

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse(message: _localizer["LogoutSuccess"]);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("SubscriptionPlan", user.SubscriptionPlan.ToString()),
                new Claim("TenantId", user.TenantId.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpireMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // حتى لو الـ email مش موجود بنرجع نفس الرسالة
            // عشان ما نكشلوش للهاكر إن الـ email موجود أو لأ
            if (user is null)
                return ApiResponse.SuccessResponse(message: _localizer["PasswordResetSent"]);

            // بيلغي أي tokens قديمة للـ user
            var oldTokens = await _uow.Read<PasswordResetToken>()
                .ListAsync(t => t.UserId == user.Id && !t.IsUsed, ct);
            foreach (var old in oldTokens)
            {
                old.MarkUsed();
                await _uow.Write<PasswordResetToken>().UpdateAsync(old, ct);
            }

            var token = new PasswordResetToken(
                user.Id,
                Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddHours(1));

            await _uow.Write<PasswordResetToken>().AddAsync(token, ct);
            await _uow.SaveChangesAsync();

            var resetLink = $"{_configuration["AppSettings:FrontendUrl"]}/reset-password?token={token.Token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

            return ApiResponse.SuccessResponse(message: _localizer["PasswordResetSent"]);
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct)
        {
            var stored = await _uow.Read<PasswordResetToken>()
                .GetSingleAsync(t => t.Token == dto.Token, ct);

            if (stored is null || !stored.IsValid)
                return ApiResponse.FailureResponse(_localizer["InvalidResetToken"], HttpStatusCode.BadRequest);

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user is null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description, HttpStatusCode.BadRequest);

            stored.MarkUsed();
            await _uow.Write<PasswordResetToken>().UpdateAsync(stored, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["PasswordResetSuccess"]);
        }
    }
}
