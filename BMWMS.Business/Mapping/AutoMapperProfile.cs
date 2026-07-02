using AutoMapper;
using BMWMS.Repository.Entities;
using BMWMS.Business.DTOs;

namespace BMWMS.Business.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Category, CategoryDto>();
        }
    }
}
