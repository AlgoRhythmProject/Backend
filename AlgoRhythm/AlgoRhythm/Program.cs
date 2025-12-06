using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Common;
using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Repositories.Courses;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Repositories.Submissions;
using AlgoRhythm.Repositories.Submissions.Interfaces;
using AlgoRhythm.Repositories.Tasks;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Services.CodeExecutor;
using AlgoRhythm.Services.CodeExecutor.Interfaces;
using AlgoRhythm.Services.Common;
using AlgoRhythm.Services.Common.Interfaces;
using AlgoRhythm.Services.Courses;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Services.Submissions;
using AlgoRhythm.Services.Submissions.Interfaces;
using AlgoRhythm.Services.Tasks;
using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Services.Users;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Database - Build connection string from environment variables or appsettings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Override with environment variables if present (for Docker)
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");

if (!string.IsNullOrEmpty(dbServer) && !string.IsNullOrEmpty(dbPassword))
{
    connectionString = $"Server={dbServer};Database={dbName ?? "master"};User Id={dbUser ?? "sa"};Password={dbPassword};TrustServerCertificate=True";
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    )
);

// ASP.NET Core Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Email settings
    options.SignIn.RequireConfirmedEmail = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<ISubmissionRepository, EfSubmissionRepository>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<ICourseRepository, EfCourseRepository>();
builder.Services.AddScoped<ILectureRepository, EfLectureRepository>();
builder.Services.AddScoped<ICourseProgressRepository, EfCourseProgressRepository>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
builder.Services.AddScoped<IHintRepository, EfHintRepository>();

builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ICodeExecutor, AlgoRhythm.Services.CodeExecutor.CodeExecutor>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILectureService, LectureService>();
builder.Services.AddScoped<ICourseProgressService, CourseProgressService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IHintService, HintService>();
builder.Services.AddSingleton<ICodeParser, CSharpCodeParser>();


// DI - clients
builder.Services.AddHttpClient<CodeExecutorClient>(client =>
{
    client.BaseAddress = new Uri("http://code_executor:8080");
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] // Najpierw User Secrets/appsettings
    ?? Environment.GetEnvironmentVariable("JWT_KEY") // Fallback: Environment Variable
    ?? throw new InvalidOperationException("JWT key is not configured. Set 'Jwt:Key' in User Secrets or 'JWT_KEY' environment variable.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;

    // Read token from cookie instead of Authorization header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Try to get token from cookie if not in header
            if (string.IsNullOrEmpty(context.Token))
            {
                context.Token = context.Request.Cookies["JWT"];
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT Authorization
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlgoRhythm API",
        Version = "v1",
        Description = "API for AlgoRhythm e-learning platform - authentication, courses, exercises"
    });

    // Add JWT Authorization to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token (without 'Bearer' prefix)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Optional: add XML comments (only if file exists)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Applying migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying migrations...");
        context.Database.Migrate(); // ensures Roles, Users, etc. exist
        logger.LogInformation("Database ready!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying migrations.");
        throw;
    }
}

// Seed default roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new Role
        {
            Name = "User",
            Description = "Default user role"
        });
    }

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new Role
        {
            Name = "Admin",
            Description = "Administrator with full access"
        });
    }
}

// Swagger UI (default path /swagger)
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();