using _360Retail.Services.Sales.API.Wrappers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.Sales.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        // Helper trả về thành công (200 OK)
        protected IActionResult OkResult<T>(T data, string message = "Operation successful")
        {
            var response = new ApiResponse<T>(data, message);
            return Ok(response);
        }

        // Helper trả về lỗi (400 Bad Request)
        protected IActionResult BadResult(string message)
        {
            var response = new ApiResponse<object>(message); // Data là null
            return BadRequest(response);
        }

        // Helper lấy thông tin User đang đăng nhập từ Token (Tech Lead trick)
        protected Guid GetCurrentUserId()
        {
            var userId = User.FindFirst("id")?.Value; // "id" là claim ta đã lưu lúc Login
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }

        protected Guid GetCurrentStoreId()
        {
            var storeId = User.FindFirst("store_id")?.Value;
            return storeId != null ? Guid.Parse(storeId) : Guid.Empty;
        }
    }
}