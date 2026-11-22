using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.UserContext;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Users;

namespace PublicSafetyHub.Application.Services;

public class UserService(
    IUserRepository repository,
    IMapper mapper,
    IValidator<PaginationRequest> paginationValidator,
    UserManager<UserEntity> userManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : IUserService
{
    public async Task<ApplicationResult<PagedResult<User>>> GetUsersAsync(FilteredRequest<UserFilter> request)
    {
        var validation = await paginationValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            return ResultHelper.Validation(validation.Errors[0].ErrorMessage);
        }

        if (request.Filter?.SearchValue?.Length > 256)
        {
            return ResultHelper.Validation("SearchValue must not exceed 256 characters.");
        }

        var specification = request.Filter is null ? new UserSpecification() : mapper.Map<UserSpecification>(request.Filter);
        return await repository.GetPagedAsync(specification, request, user => new User
        {
            Id = user.Id,
            Email = user.Email,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    public async Task<ApplicationResult<NoValueResult>> BlockUserAsync(int id, BlockUserRequest request)
    {
        if (request.IsBlocked is null)
        {
            return ResultHelper.Validation("IsBlocked value must be provided.");
        }

        return await ChangeUserStateAsync(id, isBlocked: request.IsBlocked.Value, delete: false);
    }

    public async Task<ApplicationResult<NoValueResult>> DeleteUserAsync(int id)
    {
        return await ChangeUserStateAsync(id, isBlocked: false, delete: true);
    }

    private async Task<ApplicationResult<NoValueResult>> ChangeUserStateAsync(int id, bool isBlocked, bool delete)
    {
        var currentUserId = userContext.GetUserId();

        if (currentUserId.HasValue && currentUserId.Value == id && (isBlocked || delete))
        {
            return ResultHelper.Validation("You cannot block or delete your own account through user administration.");
        }

        var user = await repository.GetAsync(id);

        if (user is null)
        {
            return ResultHelper.NotFound("User was not found.");
        }

        await unitOfWork.BeginTransactionAsync();

        if (delete)
        {
            repository.Delete(user);
        }
        else
        {
            user.IsBlocked = isBlocked;
            repository.Update(user);
        }

        if (delete || isBlocked)
        {
            var result = await userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync();
                return ResultHelper.Validation("The account has changed. Please retry the request.");
            }

            var subject = user.Id.ToString();
            await authorizationManager.RevokeBySubjectAsync(subject);
            await tokenManager.RevokeBySubjectAsync(subject);
        }

        await unitOfWork.CommitTransactionAsync();
        return NoValueResult.Value;
    }
}
