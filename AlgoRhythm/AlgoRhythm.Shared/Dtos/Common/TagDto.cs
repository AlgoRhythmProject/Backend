namespace AlgoRhythm.Shared.Dtos.Common;

public class TagDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}