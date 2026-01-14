using AlgoRhythm.Services.Admin.Interfaces;
using AlgoRhythm.Shared.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService service, ILogger<AdminController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Check if the current authenticated user is an admin (Available for all authenticated users)
    /// </summary>
    [HttpGet("is-admin")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> IsCurrentUserAdmin(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid user token" });
        }

        try
        {
            var isAdmin = await _service.IsUserAdminAsync(userId, ct);
            return Ok(new { userId, isAdmin });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all users in the system (Admin only)
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<UserWithRolesDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var users = await _service.GetAllUsersAsync(ct);
        return Ok(users);
    }

    /// <summary>
    /// Get user details with roles (Admin only)
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserWithRolesDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserWithRoles(Guid userId, CancellationToken ct)
    {
        try
        {
            var user = await _service.GetUserWithRolesAsync(userId, ct);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assign Admin role to a user (Admin only)
    /// </summary>
    [HttpPost("users/{userId:guid}/assign-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignAdminRole(Guid userId, CancellationToken ct)
    {
        try
        {
            await _service.AssignAdminRoleAsync(userId, ct);
            return Ok(new { message = "Admin role assigned successfully", userId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to assign admin role to user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke Admin role from a user (Admin only).
    /// If revoking from self, user will be logged out automatically.
    /// Cannot revoke from the last admin.
    /// </summary>
    [HttpPost("users/{userId:guid}/revoke-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RevokeAdminRole(Guid userId, CancellationToken ct)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Unauthorized(new { error = "Invalid user token" });
        }

        try
        {
            await _service.RevokeAdminRoleAsync(userId, currentUserId, ct);
            
            // Check if user is revoking their own admin role
            bool isRevokingSelf = userId == currentUserId;
            
            if (isRevokingSelf)
            {
                // Clear JWT cookie to log out the user
                Response.Cookies.Delete("JWT");
                
                _logger.LogInformation("User {UserId} revoked their own Admin role and has been logged out", userId);
                
                return Ok(new 
                { 
                    message = "Admin role revoked successfully. You have been logged out.",
                    userId,
                    loggedOut = true
                });
            }
            
            return Ok(new 
            { 
                message = "Admin role revoked successfully", 
                userId,
                loggedOut = false
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to revoke admin role from user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}