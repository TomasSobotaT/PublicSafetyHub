using AutoMapper;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Models.Logging;
using PublicSafetyHub.Models.Users;

namespace PublicSafetyHub.Application.Automapper;

public class AutomapperProfile : Profile
{
    public AutomapperProfile()
    {
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
        CreateMap<ArticleEntity, Article>();
        CreateMap<ArticleFilter, ArticleSpecification>()
            .ForMember(destination => destination.QueryEmbedding, options => options.Ignore());
        CreateMap<UserEntity, User>();
        CreateMap<LogFilter, LogSpecification>();
        CreateMap<UserFilter, UserSpecification>();
    }
}
