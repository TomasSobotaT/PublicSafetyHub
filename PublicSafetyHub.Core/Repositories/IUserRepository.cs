using Microsoft.AspNetCore.Identity;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Core.Repositories;

public interface IUserRepository : IExtendedBaseRepository<UserEntity, int>
{
    Task<IdentityResult> CreateUserAsync(UserEntity user, string password);

    Task<UserEntity> GetUserByEmailAsync(string email);

    Task<bool> UserExistsByEmailAsync(string email);
}
