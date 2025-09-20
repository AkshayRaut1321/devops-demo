using AutoMapper;
using DevOpsDemo.Domain;

namespace DevOpsDemo.Infrastructure
{
    public class InfrastructureProfile : Profile
    {
        public InfrastructureProfile()
        {
            CreateMap<ProductEntity, Product>().ReverseMap();
        }
    }
}
