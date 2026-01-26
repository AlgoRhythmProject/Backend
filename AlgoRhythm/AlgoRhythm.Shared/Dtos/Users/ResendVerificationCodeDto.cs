namespace AlgoRhythm.Shared.Dtos.Users;

/// <summary>
/// Request for resending email verification code.
/// </summary>
public class ResendVerificationCodeDto
{
    public required string Email { get; set; }
}