using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        await context.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);

        await SeedUsersAsync(userManager);
        await SeedContentAsync(context);

    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        // Create default system roles
        string[] roleNames = { "Admin", "Student" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        // Seed admin user
        var adminEmail = "admin@algorhythm.dev";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed default student accounts
        var sampleStudents = new[]
        {
            ("john.doe@algorhythm.dev", "John", "Doe"),
            ("alice@algorhythm.dev", "Alice", "Walker"),
            ("mark@algorhythm.dev", "Mark", "Roberts")
        };

        foreach (var (email, first, last) in sampleStudents)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = first,
                    LastName = last,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(user, "Student123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Student");
                }
            }
        }
    }

    private static async Task SeedContentAsync(ApplicationDbContext context)
    {
        // --- TAGS ---
        var tagAlgo = new Tag { Name = "Algorithms", Description = "Algorithmic tasks" };
        var tagIntro = new Tag { Name = "Introduction", Description = "Beginner friendly" };
        var tagCSharp = new Tag { Name = "CSharp", Description = "C# programming tasks" };
        var tagData = new Tag { Name = "DataStructures", Description = "Data structure fundamentals" };

        await context.Tags.AddRangeAsync(tagAlgo, tagIntro, tagCSharp, tagData);

        // --- COURSES ---
        var course1 = new Course
        {
            Name = "C# Programming Basics",
            Description = "Learn the fundamentals of C#, variables, loops and simple algorithms.",
            CreatedAt = DateTime.UtcNow,
            IsPublished = true
        };

        var course2 = new Course
        {
            Name = "Algorithms and Data Structures",
            Description = "Classic algorithms explained step-by-step.",
            CreatedAt = DateTime.UtcNow,
            IsPublished = true
        };

        // --- LECTURES for course1 ---
        var lecture1 = new Lecture { Title = "Welcome to C#", Course = course1 };
        var lecture2 = new Lecture { Title = "Variables and Types", Course = course1 };
        var lecture3 = new Lecture { Title = "Loops and Conditions", Course = course1 };

        // --- LECTURES for course2 ---
        var lecture4 = new Lecture { Title = "Introduction to Algorithms", Course = course2 };
        var lecture5 = new Lecture { Title = "Big-O Complexity", Course = course2 };

        // --- LECTURE CONTENT ---
        var content1 = new LectureText
        {
            Lecture = lecture1,
            Text = "### Welcome!\nThis lecture introduces you to the basics of the C# language.",
            Type = ContentType.Text
        };

        var content2 = new LecturePhoto
        {
            Lecture = lecture1,
            Path = "https://example.com/csharp.png",
            Alt = "C# Logo",
            Type = ContentType.Photo
        };

        var content3 = new LectureText
        {
            Lecture = lecture2,
            Text = "Variables in C# must have a defined type. Examples: int, string, bool.",
            Type = ContentType.Text
        };

        var content4 = new LectureText
        {
            Lecture = lecture4,
            Text = "Algorithms are step-by-step solutions to computational problems.",
            Type = ContentType.Text
        };

        // --- PROGRAMMING TASKS ---
        var task1 = new ProgrammingTaskItem
        {
            Title = "Sum of Two Numbers",
            Description = "Write a function that returns the sum of two integers.",
            Difficulty = Difficulty.Easy,
            TemplateCode = "public class Solution { public int Sum(int a, int b) { return 0; } }"
        };

        var task2 = new ProgrammingTaskItem
        {
            Title = "Find Maximum",
            Description = "Return the maximum of two numbers.",
            Difficulty = Difficulty.Easy,
            TemplateCode = "public class Solution { public int Max(int a, int b) { return 0; } }"
        };

        var task3 = new ProgrammingTaskItem
        {
            Title = "Reverse String",
            Description = "Reverse a string without using built-in reverse methods.",
            Difficulty = Difficulty.Medium,
            TemplateCode = "public string Reverse(string s) { return \"\"; }"
        };

        // --- INTERACTIVE TASK ---
        var task4 = new InteractiveTaskItem
        {
            Title = "Guess the Output",
            Description = "Look at a piece of C# code and choose the correct output.",
            Difficulty = Difficulty.Easy
        };

        // --- TEST CASES ---
        var test1 = new TestCase
        {
            ProgrammingTaskItem = task1,
            InputJson = "{ \"a\": 1, \"b\": 2 }",
            ExpectedJson = "3",
            IsVisible = true
        };

        var test2 = new TestCase
        {
            ProgrammingTaskItem = task2,
            InputJson = "{ \"a\": 5, \"b\": 3 }",
            ExpectedJson = "5",
            IsVisible = true
        };

        var test3 = new TestCase
        {
            ProgrammingTaskItem = task3,
            InputJson = "{ \"text\": \"hello\" }",
            ExpectedJson = "\"olleh\"",
            IsVisible = true
        };

        // --- TAG RELATIONS ---
        task1.Tags = new List<Tag> { tagCSharp, tagIntro };
        task2.Tags = new List<Tag> { tagCSharp };
        task3.Tags = new List<Tag> { tagAlgo };
        task4.Tags = new List<Tag> { tagIntro };

        course1.TaskItems = new List<TaskItem> { task1, task2 };
        course2.TaskItems = new List<TaskItem> { task3, task4 };

        // --- SAVE ALL ---
        await context.Courses.AddRangeAsync(course1, course2);
        await context.Lectures.AddRangeAsync(lecture1, lecture2, lecture3, lecture4, lecture5);
        await context.LectureContents.AddRangeAsync(content1, content2, content3, content4);
        await context.TaskItems.AddRangeAsync(task1, task2, task3, task4);
        await context.TestCases.AddRangeAsync(test1, test2, test3);

        await context.SaveChangesAsync();
    }
}