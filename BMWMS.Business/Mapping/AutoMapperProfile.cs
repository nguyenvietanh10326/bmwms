using AutoMapper;
using BMWMS.Repository.Entities;
using BMWMS.Business.DTOs;

namespace BMWMS.Business.Mapping
{
    public class CategoryMapperProfile : Profile
    {
        public CategoryMapperProfile()
        {
            CreateMap<Category, CategoryDto>();
        }
    }
}
