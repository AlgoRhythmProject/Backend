namespace AlgoRhythm.Shared.Dtos;

    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Difficulty { get; set; }
        public bool IsPublished { get; set; }
        public string Type { get; set; }
    }