using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Data.Repositories.Interfaces;

namespace PublicSafetyHub.Data.Repositories;

public class UserRepository(PublicSafetyHubDbContext dbContext)
    : ExtendedBaseRepository<UserEntity, int>(dbContext), IUserRepository
{
    public async Task<UserEntity> GetUserByEmailAsync(string email)
    {
        return await GetQueryable().FirstOrDefaultAsync(user => user.NormalizedEmail == email);
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await GetQueryable().IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == email);
    }
}
