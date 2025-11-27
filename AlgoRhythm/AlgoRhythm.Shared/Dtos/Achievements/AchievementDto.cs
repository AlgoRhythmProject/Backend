using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoRhythm.Shared.Dtos.Achievements;

public class AchievementDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public List<RequirementDto> Requirements { get; set; } = [];
}
