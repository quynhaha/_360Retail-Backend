using _360Retail.Services.Saas.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Saas.API.Controllers;

/// <summary>
/// AI Chatbot tư vấn gói dịch vụ 360Retail (public — không cần đăng nhập)
/// </summary>
[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly ChatbotService _chatbotService;

    public ChatbotController(ChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    /// <summary>
    /// Gửi câu hỏi cho chatbot. Hỗ trợ tư vấn gói dịch vụ, tính năng, đăng ký.
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatbotRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Vui lòng nhập câu hỏi" });

        var response = await _chatbotService.AskAsync(request.Question);

        return Ok(new
        {
            success = true,
            data = new
            {
                answer = response.Answer,
                source = response.Source,
                timestamp = response.Timestamp
            }
        });
    }

    /// <summary>
    /// Danh sách câu hỏi gợi ý (để frontend hiển thị quick buttons)
    /// </summary>
    [HttpGet("suggestions")]
    public IActionResult GetSuggestions()
    {
        var suggestions = new[]
        {
            new { text = "💰 Giá gói dịch vụ", question = "Các gói dịch vụ giá bao nhiêu?" },
            new { text = "🚀 Tính năng chính", question = "360Retail có những tính năng gì?" },
            new { text = "📊 So sánh Basic vs Pro", question = "So sánh gói Basic và Pro" },
            new { text = "🎁 Dùng thử miễn phí", question = "Làm sao để dùng thử miễn phí?" },
            new { text = "📝 Cách đăng ký", question = "Hướng dẫn đăng ký tài khoản" },
            new { text = "💳 Thanh toán", question = "Có những phương thức thanh toán nào?" }
        };

        return Ok(new { success = true, data = suggestions });
    }
}

public class ChatbotRequest
{
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi")]
    [MaxLength(500, ErrorMessage = "Câu hỏi tối đa 500 ký tự")]
    public string Question { get; set; } = null!;
}
