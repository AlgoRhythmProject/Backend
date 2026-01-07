using AlgoRhythm.Services.Admin.Interfaces;
using AlgoRhythm.Shared.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
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
    /// Get all users in the system (Admin only)
    /// </summary>
    [HttpGet("users")]
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
    /// Revoke Admin role from a user (Admin only)
    /// </summary>
    [HttpPost("users/{userId:guid}/revoke-admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RevokeAdminRole(Guid userId, CancellationToken ct)
    {
        try
        {
            await _service.RevokeAdminRoleAsync(userId, ct);
            return Ok(new { message = "Admin role revoked successfully", userId });
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