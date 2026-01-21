using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace _360Retail.Services.Saas.API.Services;

/// <summary>
/// VNPay payment integration service - API v2.1.0
/// Supports configurable HashAlgorithm (SHA512 default)
/// </summary>
public class VNPayService
{
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _baseUrl;
    private readonly string _returnUrl;
    private readonly string _hashAlgorithm;
    private readonly ILogger<VNPayService>? _logger;

    public VNPayService(IConfiguration config, ILogger<VNPayService>? logger = null)
    {
        _logger = logger;
        var vnpay = config.GetSection("VNPay");
        _tmnCode = (vnpay["TmnCode"] ?? "").Trim();
        _hashSecret = (vnpay["HashSecret"] ?? "").Trim();
        _baseUrl = vnpay["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        _returnUrl = vnpay["ReturnUrl"] ?? "http://localhost:5001/api/payments/vnpay-return";
        _hashAlgorithm = vnpay["HashAlgorithm"] ?? "SHA512";
    }

    public string CreatePaymentUrl(Guid paymentId, decimal amount, string orderInfo, string returnUrl, string ipAddress)
    {
        // 1. IP Sanitization
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress.StartsWith("::ffff:"))
        {
             ipAddress = "127.0.0.1";
        }

        // 2. Timezone GMT+7
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");

        // 3. Build Sorted Params
        var vnp_Params = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", _tmnCode },
            { "vnp_Amount", ((long)(amount * 100)).ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", paymentId.ToString() },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", string.IsNullOrEmpty(returnUrl) ? _returnUrl : returnUrl },
            { "vnp_IpAddr", ipAddress },
            { "vnp_CreateDate", createDate }
        };

        // 4. Build query string - VNPay uses urlencode (spaces become +)
        // Use WebUtility.UrlEncode which converts spaces to + (like PHP's urlencode)
        var queryBuilder = new StringBuilder();
        foreach (var kv in vnp_Params)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                if (queryBuilder.Length > 0) queryBuilder.Append('&');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));
            }
        }

        var queryString = queryBuilder.ToString();
        var vnp_SecureHash = ComputeHash(_hashSecret, queryString);

        _logger?.LogWarning("=== VNPay CreateUrl ({Algo}) ===", _hashAlgorithm);
        _logger?.LogWarning("Secret: {Secret}...", _hashSecret.Substring(0, Math.Min(5, _hashSecret.Length)));
        _logger?.LogWarning("SignData: {Data}", queryString);
        _logger?.LogWarning("Hash: {Hash}", vnp_SecureHash);

        return $"{_baseUrl}?{queryString}&vnp_SecureHash={vnp_SecureHash}";
    }

    public bool ValidateCallback(IQueryCollection queryParams, out string transactionStatus, out Guid paymentId)
    {
        transactionStatus = "";
        paymentId = Guid.Empty;

        try
        {
            if (!queryParams.ContainsKey("vnp_SecureHash")) return false;
            
            var vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();
            
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            foreach (var key in queryParams.Keys)
            {
                if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                {
                    var value = queryParams[key].ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        vnp_Params.Add(key, value);
                    }
                }
            }

            // VNPay uses urlencode (spaces become +)
            var queryBuilder = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                if (queryBuilder.Length > 0) queryBuilder.Append('&');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));
            }

            var queryString = queryBuilder.ToString();
            var checkSum = ComputeHash(_hashSecret, queryString);

            _logger?.LogWarning("=== VNPay ValidateCallback ===");
            _logger?.LogWarning("SignData: {Data}", queryString);
            _logger?.LogWarning("Our Hash: {Hash}", checkSum);
            _logger?.LogWarning("VNPay Hash: {Hash}", vnp_SecureHash);

            if (!checkSum.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogError("Invalid Signature! Client={Client} Server={Server} Algo={Algo}", vnp_SecureHash, checkSum, _hashAlgorithm);
                return false;
            }

            transactionStatus = vnp_Params.GetValueOrDefault("vnp_ResponseCode") ?? "";
            Guid.TryParse(vnp_Params.GetValueOrDefault("vnp_TxnRef") ?? "", out paymentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ValidateCallback Error");
            return false;
        }
    }

    public bool IsPaymentSuccess(string responseCode) => responseCode == "00";

    private string ComputeHash(string key, string data)
    {
        if (_hashAlgorithm.Equals("SHA256", StringComparison.OrdinalIgnoreCase))
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
        }
        else
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
        }
    }
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}
