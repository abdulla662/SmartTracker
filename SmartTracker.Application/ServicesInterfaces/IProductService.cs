using SmartTracker.Application.Common;
using SmartTracker.Application.DTOs;

namespace SmartTracker.Application.ServicesInterfaces
{
    public interface IProductService
    {
        Task<ApiResponseT<Guid>> CreateAsync(CreateProductRequestDto request);       
    }
}
