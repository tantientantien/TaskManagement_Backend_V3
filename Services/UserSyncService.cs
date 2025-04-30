using Backend.Entities;

namespace Backend.Services;

public class UserSyncService
{
    private readonly IUserService _userService;
    private readonly TaskManagementContext _dbContext;

    public UserSyncService(IUserService userService, TaskManagementContext dbContext)
    {
        _userService = userService;
        _dbContext = dbContext;
    }

    public async Task SyncUsersAsync()
    {
        var clerkUsers = await _userService.GetAllUsersAsync();
        
        foreach (var clerkUser in clerkUsers)
        {
            var localUser = await _dbContext.Users.FindAsync(clerkUser.Id);
            if (localUser == null)
            {
                localUser = new User
                {
                    Id = clerkUser.Id,
                    FirstName = clerkUser.FirstName,
                    LastName = clerkUser.LastName,
                    Email = clerkUser.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty,
                    AvatarUrl = clerkUser.ImageUrl,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(clerkUser.CreatedAt).UtcDateTime
                };
                _dbContext.Users.Add(localUser);
            }
            else
            {
                localUser.FirstName = clerkUser.FirstName;
                localUser.LastName = clerkUser.LastName;
                localUser.Email = clerkUser.EmailAddresses.FirstOrDefault()?.Email ?? string.Empty;
                localUser.AvatarUrl = clerkUser.ImageUrl;
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}