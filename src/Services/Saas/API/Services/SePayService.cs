using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace _360Retail.Services.Saas.API.Services;

/// <summary>
/// SePay bank transfer payment integration
/// Uses VietQR for QR code + SePay webhook for auto-confirmation
/// </summary>
public class SePayService
{
    private readonly string _merchantId;
    private readonly string _secretKey;
    private readonly string _bankName;
    private readonly string _accountNumber;
    private readonly string _accountName;
    private readonly ILogger<SePayService> _logger;

    public SePayService(IConfiguration config, ILogger<SePayService> logger)
    {
        _logger = logger;
        var sepay = config.GetSection("SePay");
        _merchantId = sepay["MerchantId"] ?? "";
        _secretKey = sepay["SecretKey"] ?? "";
        _bankName = sepay["BankName"] ?? "MBBank";
        _accountNumber = sepay["AccountNumber"] ?? "";
        _accountName = sepay["AccountName"] ?? "";
    }

    /// <summary>
    /// Generate a unique payment code for bank transfer content
    /// Format: 360R{last 8 chars of paymentId}
    /// </summary>
    public string GeneratePaymentCode(Guid paymentId)
    {
        var shortId = paymentId.ToString("N")[..8].ToUpper();
        return $"360R{shortId}";
    }

    /// <summary>
    /// Generate VietQR URL for bank transfer QR code
    /// Uses free VietQR API: https://img.vietqr.io
    /// </summary>
    public string GenerateQrUrl(decimal amount, string paymentCode)
    {
        var bankCode = GetBankCode(_bankName);
        var encodedInfo = Uri.EscapeDataString(paymentCode);
        return $"https://img.vietqr.io/image/{bankCode}-{_accountNumber}-compact.png?amount={amount:0}&addInfo={encodedInfo}&accountName={Uri.EscapeDataString(_accountName)}";
    }

    /// <summary>
    /// Create payment response with QR and bank info for client
    /// </summary>
    public object CreatePaymentInfo(Guid paymentId, decimal amount, string planName)
    {
        var paymentCode = GeneratePaymentCode(paymentId);
        var qrUrl = GenerateQrUrl(amount, paymentCode);

        _logger.LogInformation("SePay payment created: PaymentId={PaymentId}, Code={Code}, Amount={Amount}",
            paymentId, paymentCode, amount);

        return new
        {
            provider = "sepay",
            paymentId = paymentId,
            paymentCode = paymentCode,
            qrCodeUrl = qrUrl,
            bankInfo = new
            {
                bankName = _bankName,
                accountNumber = _accountNumber,
                accountName = _accountName,
                amount = amount,
                content = paymentCode
            },
            planName = planName,
            instruction = $"Chuyển khoản {amount:N0} VND tới {_bankName} - {_accountNumber} với nội dung: {paymentCode}"
        };
    }

    /// <summary>
    /// Extract payment code from SePay webhook transaction content
    /// Webhook content may contain extra text, we look for pattern "360R" + 8 chars
    /// </summary>
    public string? ExtractPaymentCode(string? transactionContent)
    {
        if (string.IsNullOrWhiteSpace(transactionContent))
            return null;

        // Look for pattern: 360R followed by 8 alphanumeric chars
        var index = transactionContent.IndexOf("360R", StringComparison.OrdinalIgnoreCase);
        if (index >= 0 && index + 12 <= transactionContent.Length)
        {
            return transactionContent.Substring(index, 12).ToUpper();
        }

        return null;
    }

    /// <summary>
    /// Find paymentId from payment code
    /// </summary>
    public Guid? ParsePaymentCodeToGuid(string paymentCode, IEnumerable<Guid> pendingPaymentIds)
    {
        // Short code = last 8 chars of paymentId (no hyphens)
        var shortCode = paymentCode.Replace("360R", "").ToUpper();
        
        foreach (var id in pendingPaymentIds)
        {
            var idShort = id.ToString("N")[..8].ToUpper();
            if (idShort == shortCode)
                return id;
        }

        return null;
    }

    /// <summary>
    /// Map common bank names to VietQR bank codes
    /// </summary>
    private static string GetBankCode(string bankName)
    {
        return bankName.ToUpper() switch
        {
            "MBBANK" or "MB" => "MB",
            "VIETCOMBANK" or "VCB" => "VCB",
            "TECHCOMBANK" or "TCB" => "TCB",
            "AGRIBANK" or "AGR" => "AGR",
            "BIDV" => "BIDV",
            "VIETINBANK" or "CTG" => "ICB",
            "ACB" => "ACB",
            "SACOMBANK" or "STB" => "STB",
            "VPBANK" or "VPB" => "VPB",
            "TPBANK" or "TPB" => "TPB",
            "HDBANK" or "HDB" => "HDB",
            "OCBBANK" or "OCB" => "OCB",
            "MSBANK" or "MSB" => "MSB",
            "SHBBANK" or "SHB" => "SHB",
            _ => bankName.ToUpper()
        };
    }
}
