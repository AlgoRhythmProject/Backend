namespace AlgoRhythm.Shared.Dtos.Admin;

public class AssignRoleDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!;
}