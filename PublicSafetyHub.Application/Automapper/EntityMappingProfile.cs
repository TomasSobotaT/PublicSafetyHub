using AutoMapper;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Specifications;
using PublicSafetyHub.Models.Articles.Filters;
using PublicSafetyHub.Models.Articles.Responses;
using PublicSafetyHub.Models.Auth.Responses;

namespace PublicSafetyHub.Application.Mappings;

public class EntityMappingProfile : Profile
{
    public EntityMappingProfile()
    {
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
        CreateMap<ArticleEntity, Article>();
        CreateMap<ArticleFilter, ArticleSpecification>();
        CreateMap<UserEntity, User>();
    }
}
