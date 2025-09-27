using AutoMapper;
using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Domain.Models;

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
