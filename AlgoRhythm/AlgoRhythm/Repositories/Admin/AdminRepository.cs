using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Admin.Interfaces;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Admin;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public AdminRepository(
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken ct)
    {
        return await _context.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<IList<string>> GetUserRolesAsync(User user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> AddUserToRoleAsync(User user, string roleName)
    {
        var result = await _userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveUserFromRoleAsync(User user, string roleName)
    {
        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName)
    {
        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<int> GetAdminCountAsync(CancellationToken ct)
    {
        var adminRole = await _roleManager.FindByNameAsync("Admin");
        if (adminRole == null)
            return 0;

        var usersInRole = await _userManager.GetUsersInRoleAsync("Admin");
        return usersInRole.Count;
    }
}