using _360Retail.Services.Sales.API.Wrappers; //  Wrapper 
using _360Retail.Services.Sales.API.Filters;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers
{
    [RequiresActiveSubscription]  // Block writes for expired trials
    public class CategoriesController : BaseApiController // Kế thừa Base
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] Guid? storeId, [FromQuery] bool includeInactive = false)
        {
            var targetStoreId = storeId ?? GetCurrentStoreId();
            if (targetStoreId == Guid.Empty)
                return BadResult("Store ID is required");

            var data = await _categoryService.GetAllAsync(targetStoreId, includeInactive);
            return OkResult(data);
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto request)
        {
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");

                var data = await _categoryService.CreateAsync(request, storeId);

                return OkResult(data, "Category created successfully");
            }
            catch (Exception ex)
            {
                return BadResult(ex.Message);
            }
        }

        [Authorize(Roles = "StoreOwner,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCategoryDto request)
        {
            if (id != request.Id) return BadResult("ID does not match!");
            try
            {
                var storeId = GetCurrentStoreId();
                if (storeId == Guid.Empty)
                    return BadResult("User has no store yet");

                await _categoryService.UpdateAsync(request, storeId);
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

                await _categoryService.DeleteAsync(id, storeId);
                return OkResult(true, "Deletion successful");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }
    }
}
