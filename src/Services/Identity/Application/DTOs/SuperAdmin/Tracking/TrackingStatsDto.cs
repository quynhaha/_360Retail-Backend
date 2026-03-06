namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin.Tracking;

public class DailyRegistrationStatDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FunnelStatDto
{
    public string Date { get; set; } = string.Empty;
    public long LandingPageViews { get; set; }
    public int Signups { get; set; }
    public decimal ConversionRate => LandingPageViews == 0 ? 0 : Math.Round((decimal)Signups / LandingPageViews * 100, 2);
}
