using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.Entities;
using MongoDB.Bson;

namespace DevOpsDemo.Infrastructure
{
    public class InfrastructureAutoMapperProfile : Profile
    {
        public InfrastructureAutoMapperProfile()
        {
            CreateMap<Product, ProductEntity>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Id)));
            CreateMap<ProductEntity, Product>();
            CreateMap<BsonDocument, SalesReport>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.GetValue("category", "").AsString))
                .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src => src.GetValue("totalRevenue", 0).ToDecimal()))
                .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.GetValue("orderCount", 0).ToDecimal()));
        }
    }
}
