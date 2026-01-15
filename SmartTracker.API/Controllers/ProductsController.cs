using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTracker.API.Filters;
using SmartTracker.Application.DTOs;
using SmartTracker.Application.ServicesInterfaces;

namespace SmartTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService; 

        public ProductsController(IProductService productService)  
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductRequestDto request)
        {
            var response = await _productService.CreateAsync(request);

            return StatusCode((int)response.StatusCode, response);
        }
    }
}