using _360Retail.Services.Sales.API.Wrappers; //  Wrapper 
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers
{
    [Authorize]
    public class CategoriesController : BaseApiController // Kế thừa Base
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var data = await _categoryService.GetAllAsync();
            return OkResult(data); // Tự động gói vào { success: true, data: [...] }
        }

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
                return BadResult(ex.Message); // Tự động gói vào { success: false, message: ... }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCategoryDto request)
        {
            if (id != request.Id) return BadResult("ID does not match!");
            try
            {
                await _categoryService.UpdateAsync(request);
                return OkResult(true, "Update successful");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _categoryService.DeleteAsync(id);
                return OkResult(true, "Deletion successful");
            }
            catch (Exception ex) { return BadResult(ex.Message); }
        }
    }
}