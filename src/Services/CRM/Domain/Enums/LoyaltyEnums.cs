namespace _360Retail.Services.CRM.Domain.Enums;

public enum LoyaltyRuleType
{
    PERCENT_ORDER_VALUE = 0,
    FIXED_PER_ORDER = 1,
    POINT_PER_QUANTITY = 2
}

public enum LoyaltyRuleStatus
{
    Active,
    Inactive,
    Archived
}

public enum LoyaltyTransactionType
{
    Earned,
    Redeemed,
    Adjustment,
    Expired
}
