using AutoMapper;
using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Domain.Models;

namespace DevOpsDemo.Application
{
    public class ApplicationAutoMapperProfile : Profile
    {
        public ApplicationAutoMapperProfile()
        {
            CreateMap<Product, ProductDto>().ReverseMap();
        }
    }
}
