namespace AlgoRhythm.Shared.Dtos.Users;

public class UpdateUserPreferencesDto
{
    public bool IsDarkTheme { get; set; }
    public string Language { get; set; } = "en";
}