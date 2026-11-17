using AutoMapper;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Models.Auth.Responses;

namespace PublicSafetyHub.Application.Mappings;

public class AutomapperProfile : Profile
{
    public AutomapperProfile()
    {
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
        CreateMap<ArticleEntity, Article>();
        CreateMap<ArticleFilter, ArticleSpecification>();
        CreateMap<UserEntity, User>();
    }
}
