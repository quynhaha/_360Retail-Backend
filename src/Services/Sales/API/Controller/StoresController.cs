using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Sales.API.Controllers;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers
{
    [Authorize]
    public class StoresController : BaseApiController
    {
        private readonly IStoreService _storeService;

        public StoresController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStoreDto request)
        {
            try
            {
                var id = await _storeService.CreateAsync(request);
                return OkResult(id, "Store created successfully");
            }
            catch (Exception ex)
            {
                return BadResult(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _storeService.GetAllAsync();
            return OkResult(data);
        }
    }
}