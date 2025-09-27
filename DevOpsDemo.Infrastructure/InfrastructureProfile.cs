using AutoMapper;
using DevOpsDemo.Domain.Models;
using DevOpsDemo.Infrastructure.Entities;

namespace DevOpsDemo.Infrastructure
{
    public class InfrastructureProfile : Profile
    {
        public InfrastructureProfile()
        {
            CreateMap<Product, ProductEntity>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Id)));
            CreateMap<ProductEntity, Product>();
        }
    }
}
