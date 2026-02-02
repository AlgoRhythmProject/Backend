using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Achievements;
using AlgoRhythm.Shared.Models.Submissions;
using AlgoRhythm.Shared.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

    // Custom tables
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    
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
    public DbSet<LectureVideo> LectureVideos { get; set; } = null!; 
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
    public DbSet<ExecutionError> Errors { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename tables
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles"); 
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // Configure RefreshToken
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // Configure CourseProgress cascade delete
        builder.Entity<CourseProgress>()
            .HasOne(cp => cp.Course)
            .WithMany(c => c.CourseProgresses)
            .HasForeignKey(cp => cp.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CourseProgress>()
            .HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // TPH (Table-Per-Hierarchy) dla LectureContent
        builder.Entity<LectureContent>()
            .HasDiscriminator<ContentType>(nameof(LectureContent.Type))
            .HasValue<LectureText>(ContentType.Text)
            .HasValue<LecturePhoto>(ContentType.Photo)
            .HasValue<LectureVideo>(ContentType.Video);

        // TPH dla TaskItem
        builder.Entity<TaskItem>()
            .HasDiscriminator<string>("TaskType")
            .HasValue<ProgrammingTaskItem>("Programming")
            .HasValue<InteractiveTaskItem>("Interactive");

        // TPH dla Submission
        builder.Entity<Submission>()
            .HasDiscriminator<string>("SubmissionType")
            .HasValue<ProgrammingSubmission>("Programming");

        builder.Entity<ProgrammingTaskItem>()
            .HasMany(p => p.TestCases)
            .WithOne(tc => tc.ProgrammingTaskItem)
            .HasForeignKey(tc => tc.ProgrammingTaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Unique constraints
        builder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // Many-to-many relationships
        builder.Entity<TaskItem>()
            .HasMany(t => t.Tags)
            .WithMany(tag => tag.TaskItems); 

        builder.Entity<Lecture>()
            .HasMany(l => l.Tags)
            .WithMany(tag => tag.Lectures);

        // Course <-> TaskItem (many-to-many)
        builder.Entity<Course>()
            .HasMany(c => c.TaskItems)
            .WithMany(t => t.Courses);

        // Course <-> Lecture (many-to-many)
        builder.Entity<Course>()
            .HasMany(c => c.Lectures)
            .WithMany(l => l.Courses);

        // User <-> CompletedLectures
        builder.Entity<User>()
            .HasMany(u => u.CompletedLectures)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserCompletedLectures",
                j => j.HasOne<Lecture>().WithMany().HasForeignKey("LectureId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "LectureId");
                    j.ToTable("UserCompletedLectures");
                    j.HasIndex("UserId");
                    j.HasIndex("LectureId");
                });

        //User <-> CompletedTasks
        builder.Entity<User>()
            .HasMany(u => u.CompletedTasks)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserCompletedTasks",
                j => j.HasOne<TaskItem>().WithMany().HasForeignKey("TaskItemId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "TaskItemId");
                    j.ToTable("UserCompletedTasks");
                    j.HasIndex("UserId");
                    j.HasIndex("TaskItemId");
                });

        // Disable cascade delete for conflicting relationships
        builder.Entity<TestResult>()
            .HasOne(tr => tr.TestCase)
            .WithMany(tc => tc.TestResults)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TestResult>()
            .HasMany(s => s.Errors)
            .WithOne(e => e.TestResult)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<UserRequirementProgress>()
            .HasOne(urp => urp.Requirement)
            .WithMany(r => r.UserRequirementProgresses)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<UserRequirementProgress>()
            .HasOne(urp => urp.UserAchievement)
            .WithMany(ua => ua.RequirementProgresses)
            .OnDelete(DeleteBehavior.NoAction);
    }
}