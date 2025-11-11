namespace AlgoRhythm.Dtos;

/// <summary>
/// DTO zawierający dane użytkownika zwracane w odpowiedziach API.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt
);