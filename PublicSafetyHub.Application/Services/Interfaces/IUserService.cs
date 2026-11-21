using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Users;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IUserService
{
    Task<ApplicationResult<PagedResult<User>>> GetUsersAsync(FilteredRequest<UserFilter> request);

    Task<ApplicationResult<NoValueResult>> BlockUserAsync(int id, BlockUserRequest request);

    Task<ApplicationResult<NoValueResult>> DeleteAsync(int id);
}
