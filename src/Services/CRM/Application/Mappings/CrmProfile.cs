using AutoMapper;
using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Domain.Entities;

namespace _360Retail.Services.CRM.Application.Mappings;

public class CrmProfile : Profile
{
    public CrmProfile()
    {
        // Rule Mappings
        CreateMap<CreateLoyaltyRuleDto, LoyaltyRule>()
            // Status defaults to Active unless overridden
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => _360Retail.Services.CRM.Domain.Enums.LoyaltyRuleStatus.Active));

        CreateMap<LoyaltyRule, LoyaltyRuleDto>();
    }
}
