using AutoMapper;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.DTOs.FollowUps;
using DealTrack.Application.DTOs.Payments;
using DealTrack.Application.DTOs.Profile;
using DealTrack.Application.DTOs.TenantInvite;
using DealTrack.Domain.Entities;

namespace DealTrack.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, GetProfileDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.SubscriptionPlan, opt => opt.MapFrom(src => src.SubscriptionPlan.ToString()))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber));

            CreateMap<Client, ClientResponseDto>();

            CreateMap<FollowUp, FollowUpResponseDto>()
                .ForMember(dest => dest.ClientName, opt => opt.Ignore());

            CreateMap<Payment, PaymentResponseDto>()
                .ForMember(dest => dest.ClientName, opt => opt.Ignore());

            CreateMap<TenantInvite, GetInviteDto>();
        }
    }
}
