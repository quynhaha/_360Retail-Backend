using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.Saas.API.Services;

/// <summary>
/// Hybrid AI Chatbot — FAQ cứng + Groq AI fallback (Llama 3.3 70B)
/// Chỉ trả lời về 360Retail, từ chối câu hỏi ngoài phạm vi
/// </summary>
public class ChatbotService
{
    private readonly ILogger<ChatbotService> _logger;
    private readonly string? _groqApiKey;

    // FAQ database — câu hỏi phổ biến, trả lời cứng (không tốn token)
    private static readonly Dictionary<string[], string> FaqDatabase = new()
    {
        {
            new[] { "giá", "bao nhiêu", "chi phí", "phí" },
            "💰 **Bảng giá 360Retail:**\n" +
            "• **Dùng thử (Trial)**: Miễn phí 7 ngày, đầy đủ tính năng\n" +
            "• **Gói Basic**: 199.000đ/tháng — phù hợp cửa hàng nhỏ (tối đa 10 NV)\n" +
            "• **Gói Pro**: 499.000đ/tháng — full tính năng + multi-store\n" +
            "• **Gói Yearly**: 4.990.000đ/năm (= Pro trả năm, tiết kiệm 17%)\n\n" +
            "Bạn có thể dùng thử miễn phí ngay mà không cần thẻ tín dụng!"
        },
        {
            new[] { "tính năng", "feature", "làm được gì", "có gì" },
            "🚀 **Tính năng chính của 360Retail:**\n" +
            "• 🛒 **Bán hàng (POS)**: Tạo đơn, quản lý sản phẩm, biến thể, tồn kho\n" +
            "• 👥 **Nhân sự (HR)**: Quản lý nhân viên, chấm công GPS, phân công task\n" +
            "• 💝 **Khách hàng (CRM)**: Feedback QR Code, tích điểm loyalty\n" +
            "• 📊 **Báo cáo**: Dashboard realtime, export Excel\n" +
            "• 💰 **Thanh toán**: Hỗ trợ VNPay + SePay QR\n" +
            "• 🔔 **Thông báo**: Realtime qua SignalR"
        },
        {
            new[] { "đăng ký", "tạo tài khoản", "sign up", "register" },
            "📝 **Cách đăng ký 360Retail:**\n" +
            "1. Truy cập trang đăng ký\n" +
            "2. Nhập email và mật khẩu (hoặc đăng nhập bằng Google)\n" +
            "3. Xác thực OTP qua email\n" +
            "4. Bấm 'Dùng thử miễn phí' → Tạo cửa hàng trial 7 ngày\n" +
            "5. Bắt đầu sử dụng ngay!\n\n" +
            "Không cần thẻ tín dụng, không cam kết!"
        },
        {
            new[] { "so sánh", "basic", "pro", "khác nhau", "chọn gói" },
            "📊 **So sánh gói Basic vs Pro:**\n\n" +
            "| Tính năng | Basic (199k) | Pro (499k) |\n" +
            "|-----------|:---:|:---:|\n" +
            "| Sản phẩm | 200 | Unlimited |\n" +
            "| Nhân viên | 10 | 20 |\n" +
            "| Quản lý kho | ✅ | ✅ |\n" +
            "| Dashboard & Báo cáo | ✅ | ✅ |\n" +
            "| Chấm công GPS | ❌ | ✅ |\n" +
            "| CRM & Loyalty | ❌ | ✅ |\n" +
            "| Export Excel | ❌ | ✅ |\n" +
            "| Multi-store | ❌ | ✅ |\n\n" +
            "💡 Gói Yearly = Pro trả theo năm, 4.990.000đ (tiết kiệm 17%)."
        },
        {
            new[] { "thanh toán", "payment", "trả tiền", "vnpay", "sepay", "qr" },
            "💳 **Phương thức thanh toán:**\n" +
            "• **VNPay**: Chuyển khoản ngân hàng, ví điện tử\n" +
            "• **SePay QR**: Quét mã QR để thanh toán nhanh\n\n" +
            "Sau khi thanh toán, gói được kích hoạt ngay lập tức!"
        },
        {
            new[] { "trial", "dùng thử", "miễn phí", "free" },
            "🎁 **Dùng thử miễn phí 7 ngày:**\n" +
            "• Đăng ký → Tạo cửa hàng trial → Sử dụng ngay\n" +
            "• Đầy đủ tính năng Pro trong 7 ngày\n" +
            "• Không cần thẻ tín dụng\n" +
            "• Hết trial → Chọn gói phù hợp để tiếp tục"
        },
        {
            new[] { "liên hệ", "hỗ trợ", "support", "hotline", "email" },
            "📞 **Liên hệ hỗ trợ:**\n" +
            "• Email: support@360retail.vn\n" +
            "• Zalo: 360Retail Support\n" +
            "• Giờ làm việc: 8:00 - 18:00 (Thứ 2 - Thứ 7)"
        },
        {
            new[] { "chấm công", "gps", "timekeeping" },
            "📍 **Chấm công GPS:**\n" +
            "• Nhân viên check-in bằng app → Gửi tọa độ GPS\n" +
            "• Hệ thống so sánh với vị trí cửa hàng (bán kính 500m)\n" +
            "• Tự động phát hiện đi trễ (sau 9h sáng)\n" +
            "• Hỗ trợ chụp ảnh selfie xác minh\n" +
            "• Báo cáo chấm công theo tháng, export Excel"
        },
        {
            new[] { "loyalty", "tích điểm", "khách hàng thân thiết" },
            "💝 **Chương trình Loyalty:**\n" +
            "• Tạo quy tắc tích điểm (VD: mỗi 10.000đ = 1 điểm)\n" +
            "• Khách hàng tích điểm tự động khi mua hàng\n" +
            "• Đổi điểm lấy quà/giảm giá\n" +
            "• Xếp hạng thành viên: Bronze → Silver → Gold"
        }
    };

    // System prompt — giới hạn AI chỉ trả lời về 360Retail
    private const string SystemPrompt = @"
Bạn là trợ lý AI của 360Retail — nền tảng quản lý bán lẻ SaaS dành cho cửa hàng nhỏ và vừa tại Việt Nam.

## QUY TẮC BẮT BUỘC:
1. CHỈ trả lời các câu hỏi liên quan đến 360Retail, quản lý bán lẻ, và các dịch vụ của 360Retail.
2. Nếu câu hỏi KHÔNG liên quan đến 360Retail (ví dụ: viết code, làm bài tập, hỏi chuyện cá nhân, toán học, lịch sử...), hãy từ chối lịch sự: 'Xin lỗi, tôi chỉ có thể hỗ trợ các câu hỏi liên quan đến 360Retail. Bạn có thắc mắc gì về dịch vụ của chúng tôi không?'
3. Trả lời ngắn gọn, thân thiện, bằng tiếng Việt.
4. Tối đa 150 từ mỗi câu trả lời.

## KIẾN THỨC VỀ 360RETAIL:

### Sản phẩm:
- 360Retail là nền tảng quản lý cửa hàng bán lẻ toàn diện (SaaS)
- 5 module: Bán hàng (POS), Nhân sự (HR), CRM, Kho hàng, Báo cáo
- Hỗ trợ đa cửa hàng (multi-store)

### Gói dịch vụ:
- Trial: Miễn phí 7 ngày, đầy đủ tính năng
- Basic: 199.000đ/tháng — bán hàng, kho, chấm công cơ bản, tối đa 10 nhân viên
- Pro: 499.000đ/tháng — full tính năng: CRM, loyalty, GPS, export, multi-store
- Yearly: 4.990.000đ/năm — tính năng = Pro, trả trước tiết kiệm 17%

### Tính năng nổi bật:
- POS bán hàng nhanh, quản lý biến thể sản phẩm
- Chấm công GPS + selfie
- Feedback QR Code từ khách hàng
- Loyalty tích điểm/đổi thưởng
- Dashboard realtime + export Excel
- Thanh toán VNPay + SePay QR
- Thông báo realtime (SignalR)
- Mời nhân viên qua email

### Đối tượng:
- Cửa hàng bán lẻ nhỏ và vừa
- Quán cafe, shop thời trang, cửa hàng tiện lợi, v.v.

### Về dự án:
- 360Retail là sản phẩm của team CorTexA, một nhóm sinh viên FPT University
- Dự án thuộc môn EXE — Khởi nghiệp sáng tạo
- Sứ mệnh: Giúp các chủ cửa hàng nhỏ số hóa quản lý bán lẻ với chi phí hợp lý
- Website: 360retail-cortexa.online
- Công nghệ: .NET 8 Microservices, React, PostgreSQL, Docker, Redis

### Về team CorTexA:
- Đội ngũ gồm các sinh viên năm 3-4 FPT University chuyên ngành Software Engineering
- Đam mê xây dựng sản phẩm công nghệ phục vụ doanh nghiệp nhỏ Việt Nam
- Liên hệ: support@360retail.vn
";

    public ChatbotService(IConfiguration config, ILogger<ChatbotService> logger)
    {
        _groqApiKey = config["Groq:ApiKey"];
        _logger = logger;
    }

    /// <summary>
    /// Xử lý câu hỏi: FAQ match trước, Groq AI fallback sau
    /// </summary>
    public async Task<ChatbotResponse> AskAsync(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new ChatbotResponse("Vui lòng nhập câu hỏi của bạn!", "faq");

        var normalizedQuestion = question.ToLower().Trim();

        // 1. Check FAQ trước (miễn phí, không tốn token)
        var faqAnswer = MatchFaq(normalizedQuestion);
        if (faqAnswer != null)
        {
            _logger.LogInformation("Chatbot FAQ match: {Question}", question);
            return new ChatbotResponse(faqAnswer, "faq");
        }

        // 2. Groq AI fallback (Llama 3.3 70B)
        if (string.IsNullOrEmpty(_groqApiKey))
        {
            _logger.LogWarning("Groq API key not configured, falling back to default");
            return new ChatbotResponse(
                "Cảm ơn bạn đã quan tâm! Để được tư vấn chi tiết, " +
                "vui lòng liên hệ support@360retail.vn hoặc thử hỏi về: giá gói, tính năng, cách đăng ký.",
                "fallback"
            );
        }

        try
        {
            var answer = await CallGroqAsync(question);
            _logger.LogInformation("Chatbot AI response: {Question}", question);
            return new ChatbotResponse(answer, "ai");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API error for question: {Question}", question);
            return new ChatbotResponse(
                "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại sau hoặc liên hệ support@360retail.vn.",
                "error"
            );
        }
    }

    /// <summary>
    /// Match câu hỏi với FAQ database
    /// </summary>
    private static string? MatchFaq(string question)
    {
        foreach (var (keywords, answer) in FaqDatabase)
        {
            // Nếu câu hỏi chứa >= 1 keyword → match
            if (keywords.Any(k => question.Contains(k)))
                return answer;
        }
        return null;
    }

    /// <summary>
    /// Gọi Groq API (OpenAI-compatible) với Llama 3.3 70B
    /// </summary>
    private async Task<string> CallGroqAsync(string question)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

        var url = "https://api.groq.com/openai/v1/chat/completions";

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = question }
            },
            temperature = 0.7,
            max_tokens = 500
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Groq API returned {StatusCode}: {Body}", response.StatusCode, responseText);
            throw new Exception($"Groq API error: {response.StatusCode}");
        }

        // Parse OpenAI-compatible response
        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? "Xin lỗi, tôi không thể trả lời câu hỏi này.";
    }
}

/// <summary>
/// Response model cho chatbot
/// </summary>
public class ChatbotResponse
{
    public string Answer { get; set; }

    /// <summary>
    /// Source: "faq" (FAQ cứng), "ai" (Gemini), "fallback" (mặc định), "error"
    /// </summary>
    public string Source { get; set; }
    public DateTime Timestamp { get; set; }

    public ChatbotResponse(string answer, string source)
    {
        Answer = answer;
        Source = source;
        Timestamp = DateTime.UtcNow;
    }
}
