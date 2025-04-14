namespace Backend.Services;

public class UserPermission
{
    private readonly UserManagement _userManagement;

    public UserPermission(UserManagement userManagement)
    {
        _userManagement = userManagement ?? throw new ArgumentNullException(nameof(userManagement));
    }

    public async Task<bool> CanEditAsAdminOrCreator(string userId, CancellationToken cancellationToken = default)
    {
        var (currentUser, userRoles) = await GetCurrentUserAndRolesAsync(cancellationToken);
        var isAdmin = userRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);
        var isCreator = currentUser.Id == userId;
        return isAdmin || isCreator;
    }

    public async Task<bool> CanEditAsAdminOrCreatorOrAssignee(string userId, string assigneeId, CancellationToken cancellationToken = default)
    {
        var (currentUser, userRoles) = await GetCurrentUserAndRolesAsync(cancellationToken);
        var isAdmin = userRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);
        var isCreator = currentUser.Id == userId;
        var isAssignee = currentUser.Id == assigneeId;
        return isAdmin || isCreator || isAssignee;
    }

    private async Task<(ClerkUser CurrentUser, HashSet<string> Roles)> GetCurrentUserAndRolesAsync(CancellationToken cancellationToken)
    {

        var currentUser = await _userManagement.GetCurrentUserAsync(cancellationToken);
        var userRoles = _userManagement.GetCurrentUserRoles()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (currentUser, userRoles);
    }
}