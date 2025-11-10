using AlgoRhythm.Models.Users;
using AlgoRhythm.Models.Courses;
using AlgoRhythm.Models.Tasks;
using AlgoRhythm.Models.Achievements;
using AlgoRhythm.Models.Submissions;
using AlgoRhythm.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

    // Users & Auth
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;

    // Achievements
    public DbSet<Achievement> Achievements { get; set; } = null!;
    public DbSet<Requirement> Requirements { get; set; } = null!;
    public DbSet<UserAchievement> UserAchievements { get; set; } = null!;
    public DbSet<UserRequirementProgress> UserRequirementProgresses { get; set; } = null!;

    // Courses & Lectures
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Lecture> Lectures { get; set; } = null!;
    public DbSet<LectureContent> LectureContents { get; set; } = null!;
    public DbSet<LectureText> LectureTexts { get; set; } = null!;
    public DbSet<LecturePhoto> LecturePhotos { get; set; } = null!;
    public DbSet<CourseProgress> CourseProgresses { get; set; } = null!;

    // Tasks
    public DbSet<TaskItem> TaskItems { get; set; } = null!;
    public DbSet<ProgrammingTaskItem> ProgrammingTaskItems { get; set; } = null!;
    public DbSet<InteractiveTaskItem> InteractiveTaskItems { get; set; } = null!;
    public DbSet<TestCase> TestCases { get; set; } = null!;
    public DbSet<Hint> Hints { get; set; } = null!;

    // Common
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;

    // Submissions
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<ProgrammingSubmission> ProgrammingSubmissions { get; set; } = null!;
    public DbSet<TestResult> TestResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPH (Table-Per-Hierarchy) dla LectureContent
        modelBuilder.Entity<LectureContent>()
            .HasDiscriminator<ContentType>(nameof(LectureContent.Type))
            .HasValue<LectureText>(ContentType.Text)
            .HasValue<LecturePhoto>(ContentType.Photo);

        // TPH dla TaskItem
        modelBuilder.Entity<TaskItem>()
            .HasDiscriminator<string>("TaskType")
            .HasValue<ProgrammingTaskItem>("Programming")
            .HasValue<InteractiveTaskItem>("Interactive");

        // TPH dla Submission
        modelBuilder.Entity<Submission>()
            .HasDiscriminator<string>("SubmissionType")
            .HasValue<ProgrammingSubmission>("Programming");

        // Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // Many-to-many relationships (EF Core 5+ automatically creates junction tables)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Permissions)
            .WithMany(p => p.Roles);

        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.Tags)
            .WithMany(tag => tag.TaskItems);

        modelBuilder.Entity<Lecture>()
            .HasMany(l => l.Tags)
            .WithMany(tag => tag.Lectures);

        modelBuilder.Entity<Course>()
            .HasMany(c => c.TaskItems)
            .WithMany(t => t.Courses);
    }
}