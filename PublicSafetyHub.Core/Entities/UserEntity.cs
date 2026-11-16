using Microsoft.AspNetCore.Identity;
using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Core.Entities;

public class UserEntity : IdentityUser<int>, IExtendedBaseEntity<int>
{
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsBlocked { get; set; }
}
