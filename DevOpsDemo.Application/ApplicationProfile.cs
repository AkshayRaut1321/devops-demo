using AutoMapper;
using DevOpsDemo.Domain;
using DevOpsDemo.Application.DTOs;

namespace DevOpsDemo.Application
{
    public class ApplicationProfile : Profile
    {
        public ApplicationProfile()
        {
            CreateMap<Product, ProductDto>().ReverseMap();
        }
    }
}
