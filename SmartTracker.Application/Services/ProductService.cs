using SmartTracker.Application.Common;
using SmartTracker.Application.DTOs;
using SmartTracker.Application.Interfaces;
using SmartTracker.Application.ServicesInterfaces;
using SmartTracker.Domain.Entities;

namespace SmartTracker.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponseT<Guid>> CreateAsync(CreateProductRequestDto request)
        {
      
                var product = new Product(request.Name, request.Price);
                await _unitOfWork.Write<Product>().AddAsync(product);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponseT<Guid>.SuccessResponse(
                    product.Id,
                    "Product created successfully" // ana h3ml localization hna y3ny al messages di httrgm bs odam isa
                );
            }  
            }
        }