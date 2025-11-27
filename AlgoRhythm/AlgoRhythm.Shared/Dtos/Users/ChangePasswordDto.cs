namespace AlgoRhythm.Shared.Dtos.Users;

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}