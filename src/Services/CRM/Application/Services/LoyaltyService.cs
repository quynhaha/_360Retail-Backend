using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Domain.Entities;
using _360Retail.Services.CRM.Domain.Enums;
using _360Retail.Services.CRM.Application.Interfaces;

namespace _360Retail.Services.CRM.Application.Services;

public interface ILoyaltyService
{
    Task<CustomerLoyaltySummaryDto> GetCustomerSummaryAsync(Guid customerId);
    Task ProcessEarnPointsAsync(Guid storeId, EarnPointsRequestDto request);
    Task ProcessRedeemPointsAsync(Guid storeId, RedeemPointsRequestDto request);
    Task<PagedResult<LoyaltyTransactionDto>> GetTransactionsAsync(Guid customerId, int page, int pageSize);
}

public class LoyaltyService : ILoyaltyService
{
    private readonly ICustomerRepository _customerRepo;
    private readonly ILoyaltyTransactionRepository _transactionRepo;
    private readonly ILoyaltyRuleRepository _ruleRepo;

    public LoyaltyService(
        ICustomerRepository customerRepo,
        ILoyaltyTransactionRepository transactionRepo,
        ILoyaltyRuleRepository ruleRepo)
    {
        _customerRepo = customerRepo;
        _transactionRepo = transactionRepo;
        _ruleRepo = ruleRepo;
    }

    public async Task<CustomerLoyaltySummaryDto> GetCustomerSummaryAsync(Guid customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found");

        return new CustomerLoyaltySummaryDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            TotalPoints = customer.TotalPoints ?? 0,
            Rank = customer.Rank ?? "Bronze"
        };
    }

    public async Task ProcessEarnPointsAsync(Guid storeId, EarnPointsRequestDto request)
    {
        if (await _transactionRepo.ExistsByOrderIdAsync(request.OrderId))
        {
            return; // Order already processed
        }

        var customer = await _customerRepo.GetByIdAsync(request.CustomerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found");

        // Find active rules for the store
        var rules = await _ruleRepo.GetByStoreIdAsync(storeId);
        var activeRules = rules.Where(r => r.Status == LoyaltyRuleStatus.Active).ToList();

        int totalPointsToEarn = 0;
        Guid? applicableRuleId = null; 

        foreach (var rule in activeRules)
        {
            int points = 0;
            switch (rule.Type)
            {
                case LoyaltyRuleType.PERCENT_ORDER_VALUE:
                    // % of order value
                    if (request.TotalAmount >= rule.MinSpend)
                    {
                        points = (int)Math.Round(request.TotalAmount * rule.EarningRate);
                    }
                    break;
                case LoyaltyRuleType.FIXED_PER_ORDER: 
                     // Fixed amount per order
                     if (request.TotalAmount >= rule.MinSpend)
                     {
                         points = (int)Math.Round(rule.EarningRate);
                     }
                    break;
                case LoyaltyRuleType.POINT_PER_QUANTITY: 
                     // Points based on product quantity
                     if (request.TotalAmount >= rule.MinSpend)
                     {
                        points = (int)Math.Round(request.TotalQuantity * rule.EarningRate);
                     }
                    break;
            }

            if (points > 0)
            {
                totalPointsToEarn += points;
                applicableRuleId = rule.Id; // Just track one for the transaction FK, or we might need split transactions
            }
        }

        if (totalPointsToEarn > 0)
        {
            customer.TotalPoints = (customer.TotalPoints ?? 0) + totalPointsToEarn;
            await _customerRepo.UpdateAsync(customer);

            var transaction = new LoyaltyTransaction
            {
                CustomerId = customer.Id,
                StoreId = storeId,
                OrderId = request.OrderId,
                RuleId = applicableRuleId,
                Points = totalPointsToEarn,
                Type = LoyaltyTransactionType.Earned,
                Description = $"Earned from Order {request.OrderId}"
            };
            await _transactionRepo.AddAsync(transaction);
        }
    }

    public async Task ProcessRedeemPointsAsync(Guid storeId, RedeemPointsRequestDto request)
    {
        var customer = await _customerRepo.GetByIdAsync(request.CustomerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found");

        if ((customer.TotalPoints ?? 0) < request.PointsToRedeem)
            throw new InvalidOperationException("Insufficient points");

        customer.TotalPoints -= request.PointsToRedeem;
        await _customerRepo.UpdateAsync(customer);

        var transaction = new LoyaltyTransaction
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            Points = -request.PointsToRedeem,
            Type = LoyaltyTransactionType.Redeemed,
            Description = "Points Redemption"
        };
        await _transactionRepo.AddAsync(transaction);
    }

    public async Task<PagedResult<LoyaltyTransactionDto>> GetTransactionsAsync(Guid customerId, int page, int pageSize)
    {
        var transactions = await _transactionRepo.GetByCustomerIdAsync(customerId, page, pageSize);
        var total = await _transactionRepo.GetTotalCountAsync(customerId);

        var dtos = transactions.Select(t => new LoyaltyTransactionDto
        {
            Id = t.Id,
            Points = t.Points,
            Type = t.Type,
            Description = t.Description,
            TransactionDate = t.TransactionDate
        });

        return new PagedResult<LoyaltyTransactionDto>(dtos, page, pageSize, total);
    }
}
