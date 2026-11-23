using AutoMapper;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Models.Logging;
using PublicSafetyHub.Models.Users;
using PublicSafetyHub.Models.Documents;
using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Application.Automapper;

public class AutomapperProfile : Profile
{
    public AutomapperProfile()
    {
        MapArticle(this);
        MapUser(this);
        MapLog(this);
        MapDocument(this);
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
    }

    private static void MapArticle(IProfileExpression profile)
    {
        profile.CreateMap<ArticleEntity, Article>();
        profile.CreateMap<ArticleFilter, ArticleSpecification>()
            .ForMember(destination => destination.QueryEmbedding, options => options.Ignore());
    }

    private static void MapUser(IProfileExpression profile)
    {
        profile.CreateMap<UserEntity, User>();
        profile.CreateMap<UserFilter, UserSpecification>();
    }

    private static void MapLog(IProfileExpression profile)
    {
        profile.CreateMap<LogEntity, Log>()
            .ForMember(destination => destination.HasException,
                options => options.MapFrom(source => !string.IsNullOrWhiteSpace(source.Exception)));
        profile.CreateMap<LogFilter, LogSpecification>();
    }

    private static void MapDocument(IProfileExpression profile)
    {
        profile.CreateMap<Document, SearchDocument>().ReverseMap();
    }
}
