namespace AlgoRhythm.Shared.Dtos.Users;

public class UserPreferencesDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsDarkTheme { get; set; }
    public string Language { get; set; } = "en";
}