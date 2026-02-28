namespace _360Retail.Shared.Email;

/// <summary>
/// Renders branded HTML email templates for 360Retail.
/// Brand colors: Mint #7ED4C4, Blue #89C5DE, Lavender #B89AC5, Navy #1E2A3A
/// </summary>
public static class EmailTemplateService
{
    // ─────────────────────────────────────────────────────────
    // BASE LAYOUT
    // ─────────────────────────────────────────────────────────
    
    private static string WrapInLayout(string title, string bodyContent, string? preheader = null)
    {
        return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <!--[if mso]>
    <style>table,td,div,p {{font-family: Arial, sans-serif;}}</style>
    <![endif]-->
</head>
<body style=""margin:0; padding:0; background-color:#f0f4f8; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;"">
    {(preheader != null ? $@"<div style=""display:none;max-height:0;overflow:hidden;"">{preheader}</div>" : "")}
    
    <!-- Wrapper -->
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f0f4f8;"">
        <tr>
            <td align=""center"" style=""padding: 30px 16px;"">
                
                <!-- Main Container -->
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px; width:100%;"">
                    
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1E2A3A 0%, #2D3F54 100%); padding: 28px 40px; border-radius: 16px 16px 0 0; text-align: center;"">
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"">
                                        <!-- Text Logo with Brand Colors -->
                                        <div style=""font-size: 28px; font-weight: 800; letter-spacing: -0.5px;"">
                                            <span style=""color: #7ED4C4;"">360</span><span style=""color: #89C5DE;"">Retail</span>
                                        </div>
                                        <div style=""color: #8899AA; font-size: 11px; letter-spacing: 2px; margin-top: 4px; text-transform: uppercase;"">Smart Retail Platform</div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Body -->
                    <tr>
                        <td style=""background-color: #ffffff; padding: 40px;"">
                            {bodyContent}
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8fafc; padding: 24px 40px; border-radius: 0 0 16px 16px; border-top: 1px solid #e2e8f0;"">
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"">
                                        <p style=""margin: 0 0 8px; color: #94a3b8; font-size: 12px;"">
                                            © {DateTime.UtcNow.Year} 360Retail · Nền tảng quản lý bán lẻ thông minh
                                        </p>
                                        <p style=""margin: 0; color: #cbd5e1; font-size: 11px;"">
                                            Email tự động — Vui lòng không trả lời email này
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    // ─────────────────────────────────────────────────────────
    // SHARED COMPONENTS
    // ─────────────────────────────────────────────────────────
    
    private static string InfoRow(string label, string value, string? valueColor = null)
    {
        var color = valueColor ?? "#1e293b";
        return $@"
            <tr>
                <td style=""padding: 10px 16px; color: #64748b; font-size: 14px; width: 140px; border-bottom: 1px solid #f1f5f9;"">{label}</td>
                <td style=""padding: 10px 16px; color: {color}; font-size: 14px; font-weight: 600; border-bottom: 1px solid #f1f5f9;"">{value}</td>
            </tr>";
    }

    private static string PrimaryButton(string text, string url)
    {
        return $@"
            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin: 28px auto 0;"">
                <tr>
                    <td align=""center"" style=""border-radius: 10px; background: linear-gradient(135deg, #7ED4C4 0%, #89C5DE 100%);"">
                        <a href=""{url}"" target=""_blank"" style=""display: inline-block; padding: 14px 36px; color: #1E2A3A; font-size: 15px; font-weight: 700; text-decoration: none; letter-spacing: 0.3px;"">
                            {text}
                        </a>
                    </td>
                </tr>
            </table>";
    }

    private static string IconBadge(string emoji, string bgColor = "#f0fdf9")
    {
        return $@"<div style=""width: 56px; height: 56px; border-radius: 14px; background: {bgColor}; display: inline-block; text-align: center; line-height: 56px; font-size: 28px; margin-bottom: 20px;"">{emoji}</div>";
    }

    // ─────────────────────────────────────────────────────────
    // TEMPLATE 1: STAFF INVITE
    // ─────────────────────────────────────────────────────────
    
    public static string StaffInvite(string employeeName, string storeName, string role, string tempPassword, string loginUrl = "http://localhost:3000/login")
    {
        var body = $@"
            <div style=""text-align: center;"">
                {IconBadge("🎉", "#f0fdf9")}
                <h1 style=""margin: 0 0 8px; font-size: 22px; color: #1e293b; font-weight: 700;"">Chào mừng đến 360Retail!</h1>
                <p style=""margin: 0 0 28px; color: #64748b; font-size: 15px;"">Bạn đã được mời vào cửa hàng</p>
            </div>

            <!-- Info Card -->
            <div style=""background: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;"">
                <div style=""background: linear-gradient(135deg, #7ED4C4 0%, #89C5DE 100%); padding: 14px 20px;"">
                    <span style=""color: #1E2A3A; font-weight: 700; font-size: 14px;"">📋 Thông tin tài khoản</span>
                </div>
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    {InfoRow("Họ tên", employeeName)}
                    {InfoRow("Cửa hàng", storeName)}
                    {InfoRow("Vai trò", role, "#7ED4C4")}
                </table>
            </div>

            <!-- Password Box -->
            <div style=""margin: 24px 0; padding: 20px; background: linear-gradient(135deg, #fef9ec 0%, #fff7ed 100%); border-radius: 12px; border: 1px solid #fde68a; text-align: center;"">
                <p style=""margin: 0 0 8px; color: #92400e; font-size: 13px; font-weight: 600;"">🔑 Mật khẩu tạm thời</p>
                <p style=""margin: 0; font-size: 28px; font-weight: 800; color: #1e293b; letter-spacing: 3px; font-family: 'Courier New', monospace;"">{tempPassword}</p>
                <p style=""margin: 8px 0 0; color: #b45309; font-size: 12px;"">⚠️ Vui lòng đổi mật khẩu ngay sau khi đăng nhập</p>
            </div>

            {PrimaryButton("Đăng nhập ngay →", loginUrl)}";

        return WrapInLayout("Chào mừng đến 360Retail", body, "Bạn đã được mời vào cửa hàng trên 360Retail");
    }

    // ─────────────────────────────────────────────────────────
    // TEMPLATE 2: TASK ASSIGNMENT
    // ─────────────────────────────────────────────────────────
    
    public static string TaskAssignment(string assigneeName, string taskTitle, string priority, string? description, DateTime? deadline, string tasksUrl = "http://localhost:3000/tasks")
    {
        var priorityColor = priority.ToLower() switch
        {
            "high" or "urgent" => "#dc2626",
            "medium" => "#f59e0b",
            "low" => "#10b981",
            _ => "#64748b"
        };
        var priorityBg = priority.ToLower() switch
        {
            "high" or "urgent" => "#fef2f2",
            "medium" => "#fffbeb",
            "low" => "#f0fdf4",
            _ => "#f8fafc"
        };
        var priorityEmoji = priority.ToLower() switch
        {
            "high" or "urgent" => "🔴",
            "medium" => "🟡",
            "low" => "🟢",
            _ => "⚪"
        };
        var deadlineText = deadline?.ToString("dd/MM/yyyy HH:mm") ?? "Không có deadline";
        var descText = string.IsNullOrEmpty(description) ? "Không có mô tả" : description;

        var body = $@"
            <div style=""text-align: center;"">
                {IconBadge("📋", "#eff6ff")}
                <h1 style=""margin: 0 0 8px; font-size: 22px; color: #1e293b; font-weight: 700;"">Task mới được giao</h1>
                <p style=""margin: 0 0 28px; color: #64748b; font-size: 15px;"">Hi <strong>{assigneeName}</strong>, bạn có task mới!</p>
            </div>

            <!-- Task Card -->
            <div style=""background: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;"">
                <div style=""padding: 20px; border-bottom: 1px solid #e2e8f0;"">
                    <h2 style=""margin: 0; font-size: 18px; color: #1e293b;"">{taskTitle}</h2>
                </div>
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    {InfoRow("Mức độ", $@"<span style='background:{priorityBg}; color:{priorityColor}; padding:3px 10px; border-radius:20px; font-size:13px;'>{priorityEmoji} {priority}</span>")}
                    {InfoRow("Deadline", deadlineText)}
                </table>
                <div style=""padding: 16px; border-top: 1px solid #f1f5f9;"">
                    <p style=""margin: 0 0 4px; color: #64748b; font-size: 13px; font-weight: 600;"">Mô tả:</p>
                    <p style=""margin: 0; color: #374151; font-size: 14px; line-height: 1.6;"">{descText}</p>
                </div>
            </div>

            {PrimaryButton("Xem task →", tasksUrl)}";

        return WrapInLayout($"Task mới: {taskTitle}", body, $"Bạn được giao task: {taskTitle}");
    }

    // ─────────────────────────────────────────────────────────
    // TEMPLATE 3: LOW STOCK ALERT
    // ─────────────────────────────────────────────────────────
    
    public static string LowStockAlert(string storeName, List<LowStockItem> items, string dashboardUrl = "http://localhost:3000/inventory")
    {
        var outOfStock = items.Where(i => i.CurrentStock <= 0).ToList();
        var lowStock = items.Where(i => i.CurrentStock > 0).ToList();
        
        var itemRows = "";
        foreach (var item in items)
        {
            var stockColor = item.CurrentStock <= 0 ? "#dc2626" : "#f59e0b";
            var stockBg = item.CurrentStock <= 0 ? "#fef2f2" : "#fffbeb";
            var stockLabel = item.CurrentStock <= 0 ? "Hết hàng" : $"Còn {item.CurrentStock}";
            var stockEmoji = item.CurrentStock <= 0 ? "🔴" : "⚠️";
            itemRows += $@"
                <tr>
                    <td style=""padding: 12px 16px; border-bottom: 1px solid #f1f5f9;"">
                        <strong style=""color: #1e293b; font-size: 14px;"">{item.ProductName}</strong>
                        {(!string.IsNullOrEmpty(item.Sku) ? $"<br><span style='color:#94a3b8; font-size:12px;'>SKU: {item.Sku}</span>" : "")}
                    </td>
                    <td style=""padding: 12px 16px; border-bottom: 1px solid #f1f5f9; text-align: right;"">
                        <span style=""background:{stockBg}; color:{stockColor}; padding:4px 12px; border-radius:20px; font-size:13px; font-weight:600;"">{stockEmoji} {stockLabel}</span>
                    </td>
                </tr>";
        }

        var body = $@"
            <div style=""text-align: center;"">
                {IconBadge("📦", "#fff7ed")}
                <h1 style=""margin: 0 0 8px; font-size: 22px; color: #1e293b; font-weight: 700;"">Cảnh báo tồn kho</h1>
                <p style=""margin: 0 0 8px; color: #64748b; font-size: 15px;"">Cửa hàng <strong>{storeName}</strong></p>
                <p style=""margin: 0 0 28px; color: #94a3b8; font-size: 13px;"">Có {items.Count} sản phẩm cần nhập thêm hàng</p>
            </div>

            <!-- Summary Badges -->
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 20px;"">
                <tr>
                    <td width=""50%"" style=""padding: 0 6px 0 0;"">
                        <div style=""background: #fef2f2; border-radius: 12px; padding: 16px; text-align: center; border: 1px solid #fecaca;"">
                            <div style=""font-size: 24px; font-weight: 800; color: #dc2626;"">{outOfStock.Count}</div>
                            <div style=""font-size: 12px; color: #991b1b; margin-top: 4px;"">Hết hàng</div>
                        </div>
                    </td>
                    <td width=""50%"" style=""padding: 0 0 0 6px;"">
                        <div style=""background: #fffbeb; border-radius: 12px; padding: 16px; text-align: center; border: 1px solid #fde68a;"">
                            <div style=""font-size: 24px; font-weight: 800; color: #f59e0b;"">{lowStock.Count}</div>
                            <div style=""font-size: 12px; color: #92400e; margin-top: 4px;"">Sắp hết</div>
                        </div>
                    </td>
                </tr>
            </table>

            <!-- Items Table -->
            <div style=""background: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;"">
                <div style=""background: linear-gradient(135deg, #f59e0b 0%, #ef4444 100%); padding: 14px 20px;"">
                    <span style=""color: #ffffff; font-weight: 700; font-size: 14px;"">📋 Danh sách sản phẩm</span>
                </div>
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    {itemRows}
                </table>
            </div>

            {PrimaryButton("Kiểm tra kho →", dashboardUrl)}";

        return WrapInLayout("⚠️ Cảnh báo tồn kho", body, $"Có {items.Count} sản phẩm sắp hết hàng tại {storeName}");
    }

    // ─────────────────────────────────────────────────────────
    // TEMPLATE 4: SUBSCRIPTION EXPIRY
    // ─────────────────────────────────────────────────────────
    
    public static string SubscriptionExpiry(string ownerName, string storeName, string planName, DateTime expiryDate, int daysRemaining, string renewUrl = "http://localhost:3000/subscription")
    {
        var urgencyColor = daysRemaining <= 1 ? "#dc2626" : daysRemaining <= 3 ? "#f59e0b" : "#3b82f6";
        var urgencyBg = daysRemaining <= 1 ? "#fef2f2" : daysRemaining <= 3 ? "#fffbeb" : "#eff6ff";
        var urgencyEmoji = daysRemaining <= 1 ? "🔴" : daysRemaining <= 3 ? "🟡" : "🔵";
        var urgencyText = daysRemaining <= 0 ? "Đã hết hạn!" : daysRemaining == 1 ? "Hết hạn ngày mai!" : $"Còn {daysRemaining} ngày";

        var body = $@"
            <div style=""text-align: center;"">
                {IconBadge("⏰", urgencyBg)}
                <h1 style=""margin: 0 0 8px; font-size: 22px; color: #1e293b; font-weight: 700;"">Gói dịch vụ sắp hết hạn</h1>
                <p style=""margin: 0 0 28px; color: #64748b; font-size: 15px;"">Xin chào <strong>{ownerName}</strong></p>
            </div>

            <!-- Countdown Badge -->
            <div style=""text-align: center; margin-bottom: 24px;"">
                <div style=""display: inline-block; background: {urgencyBg}; border: 2px solid {urgencyColor}; border-radius: 16px; padding: 16px 32px;"">
                    <div style=""font-size: 13px; color: {urgencyColor}; font-weight: 600;"">{urgencyEmoji} {urgencyText}</div>
                </div>
            </div>

            <!-- Plan Details -->
            <div style=""background: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0;"">
                <div style=""background: linear-gradient(135deg, #7ED4C4 0%, #B89AC5 100%); padding: 14px 20px;"">
                    <span style=""color: #1E2A3A; font-weight: 700; font-size: 14px;"">📦 Chi tiết gói dịch vụ</span>
                </div>
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                    {InfoRow("Cửa hàng", storeName)}
                    {InfoRow("Gói hiện tại", planName, "#7ED4C4")}
                    {InfoRow("Ngày hết hạn", expiryDate.ToString("dd/MM/yyyy"), urgencyColor)}
                </table>
            </div>

            <!-- Warning -->
            <div style=""margin: 24px 0; padding: 16px 20px; background: #fffbeb; border-radius: 12px; border: 1px solid #fde68a;"">
                <p style=""margin: 0; color: #92400e; font-size: 14px; line-height: 1.6;"">
                    ⚠️ Khi hết hạn, cửa hàng sẽ <strong>tạm khóa</strong> cho đến khi gia hạn. Dữ liệu vẫn được bảo toàn.
                </p>
            </div>

            {PrimaryButton("Gia hạn ngay →", renewUrl)}";

        return WrapInLayout("⏰ Gói dịch vụ sắp hết hạn", body, $"Gói {planName} của {storeName} sẽ hết hạn trong {daysRemaining} ngày");
    }

    // ─────────────────────────────────────────────────────────
    // TEMPLATE 5: FORGOT PASSWORD
    // ─────────────────────────────────────────────────────────
    
    public static string ForgotPassword(string userName, string resetCode, int expiryMinutes = 15, string resetUrl = "http://localhost:3000/reset-password")
    {
        var fullResetUrl = $"{resetUrl}?code={resetCode}";

        var body = $@"
            <div style=""text-align: center;"">
                {IconBadge("🔐", "#eff6ff")}
                <h1 style=""margin: 0 0 8px; font-size: 22px; color: #1e293b; font-weight: 700;"">Đặt lại mật khẩu</h1>
                <p style=""margin: 0 0 28px; color: #64748b; font-size: 15px;"">Xin chào <strong>{userName}</strong></p>
            </div>

            <p style=""color: #374151; font-size: 15px; line-height: 1.6; margin: 0 0 24px;"">
                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Sử dụng mã bên dưới để tiếp tục:
            </p>

            <!-- Reset Code Box -->
            <div style=""margin: 0 0 24px; padding: 24px; background: linear-gradient(135deg, #eff6ff 0%, #f0fdf9 100%); border-radius: 12px; border: 1px solid #bfdbfe; text-align: center;"">
                <p style=""margin: 0 0 8px; color: #1e40af; font-size: 13px; font-weight: 600;"">Mã xác nhận</p>
                <p style=""margin: 0; font-size: 36px; font-weight: 800; color: #1e293b; letter-spacing: 8px; font-family: 'Courier New', monospace;"">{resetCode}</p>
                <p style=""margin: 12px 0 0; color: #64748b; font-size: 12px;"">⏱️ Mã có hiệu lực trong {expiryMinutes} phút</p>
            </div>

            {PrimaryButton("Đặt lại mật khẩu →", fullResetUrl)}

            <!-- Security Notice -->
            <div style=""margin: 28px 0 0; padding: 16px 20px; background: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0;"">
                <p style=""margin: 0; color: #64748b; font-size: 13px; line-height: 1.6;"">
                    🛡️ Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.
                </p>
            </div>";

        return WrapInLayout("Đặt lại mật khẩu - 360Retail", body, "Mã xác nhận đặt lại mật khẩu 360Retail");
    }
}

/// <summary>
/// Represents a product with low stock for email notification
/// </summary>
public class LowStockItem
{
    public string ProductName { get; set; } = "";
    public string? Sku { get; set; }
    public int CurrentStock { get; set; }
}
