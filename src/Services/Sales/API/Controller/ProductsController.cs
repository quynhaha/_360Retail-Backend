using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Sales.API.Controllers; // BaseApiController
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers
{
  [Authorize(Roles = "StoreOwner,Manager,Staff")]
    public class ProductsController : BaseApiController
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? keyword, 
            [FromQuery] Guid? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            var storeId = GetCurrentStoreId();
            if (storeId == Guid.Empty)
                return BadResult("User has no store yet");

            var data = await _productService.GetAllAsync(storeId, keyword, categoryId, page, pageSize, includeInactive);
            return OkResult(data);
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");
                var data = await _productService.GetByIdAsync(id, storeId);
                return OkResult(data);
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpPost]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> Create([FromForm] CreateProductDto request) //  [FromForm]
        {
            if (!ModelState.IsValid)
                return BadResult("Invalid product data");
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");

                var id = await _productService.CreateAsync(request, storeId);

                return OkResult(id, "Product created successfully");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateProductDto request)
        {
            if (!ModelState.IsValid)
                return BadResult("Invalid product data");
            if (id != request.Id) return BadResult("ID does not match");
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");
                await _productService.UpdateAsync(request, storeId);
                return OkResult(true, "Update successful");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");
                await _productService.DeleteAsync(id, storeId);
                return OkResult(true, "Deletion successful");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }
    }
}