using AutoMapper;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Domain.Entities;

namespace _360Retail.Services.Sales.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Product Mappings ---
            // 1. Entity -> DTO (Read)
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : "N/A"))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants))
                .ForMember(dest => dest.TotalStock, opt => opt.MapFrom(src => src.TotalStock))
                .ForMember(dest => dest.HasVariants, opt => opt.MapFrom(src => src.HasVariants))
                .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.IsInStock))
                .ReverseMap();

            // 2. CreateDTO -> Entity (Create) - Variants handled manually in ProductService
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) 
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.ProductVariants, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Store, opt => opt.Ignore());

            // 3. UpdateDTO -> Entity (Update)
            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) 
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // --- Variant Mappings ---
            CreateMap<CreateProductVariantDto, ProductVariant>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<ProductVariant, ProductVariantDto>();


            // --- Category Mappings ---
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.CategoryName : "Origin"));

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));


        }
    }
}