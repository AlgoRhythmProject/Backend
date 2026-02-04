using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Achievements;
using AlgoRhythm.Repositories.Achievements.Interfaces;
using AlgoRhythm.Repositories.Admin;
using AlgoRhythm.Repositories.Admin.Interfaces;
using AlgoRhythm.Repositories.Common;
using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Repositories.Courses;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Repositories.Submissions;
using AlgoRhythm.Repositories.Submissions.Interfaces;
using AlgoRhythm.Repositories.Tasks;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Repositories.Users;
using AlgoRhythm.Repositories.Users.Interfaces;
using AlgoRhythm.Services.Achievements;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Services.Admin;
using AlgoRhythm.Services.Admin.Interfaces;
using AlgoRhythm.Services.Blob;
using AlgoRhythm.Services.Blob.Interfaces;
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
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace AlgoRhythm.Extensions
{
    /// <summary>
    /// Helper class for better Program.cs maintanability
    /// </summary>
    public static class WebApplicationBuilderExtension
    {
        public static void AddFrontendCors(this WebApplicationBuilder builder)
        {
            string frontedUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                            ?? throw new InvalidOperationException("Frontend url must be set!");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(frontedUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        public static void ConfigureServices(this WebApplicationBuilder builder) 
        {
            builder.Services.AddHealthChecks();

            // Repositories
            builder.Services.AddScoped<ISubmissionRepository, EfSubmissionRepository>();
            builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
            builder.Services.AddScoped<ICourseRepository, EfCourseRepository>();
            builder.Services.AddScoped<ILectureRepository, EfLectureRepository>();
            builder.Services.AddScoped<ICourseProgressRepository, EfCourseProgressRepository>();
            builder.Services.AddScoped<ITagRepository, EfTagRepository>();
            builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
            builder.Services.AddScoped<IHintRepository, EfHintRepository>();
            builder.Services.AddScoped<IAchievementRepository, EfAchievementRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ITestCaseRepository, EfTestCaseRepository>();
            builder.Services.AddScoped<IUserStreakRepository, EfUserStreakRepository>();

            // General services
            builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISubmissionService, SubmissionService>();
            builder.Services.AddScoped<ICodeExecutor, CodeExecutorService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<ILectureService, LectureService>();
            builder.Services.AddScoped<ICourseProgressService, CourseProgressService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddScoped<IHintService, HintService>();
            builder.Services.AddScoped<IAchievementService, AchievementService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<ITestCaseService, TestCaseService>();
            builder.Services.AddScoped<IUserStreakService, UserStreakService>();
            builder.Services.AddSingleton<ICodeParser, CSharpCodeParser>();
            builder.Services.AddSingleton<IFileStorageService, BlobStorageService>();
        }

        public static void ConfigureClients(this WebApplicationBuilder builder)
        {
            // Blob
            string blobConnectionString = Environment.GetEnvironmentVariable("AZURE_CONNECTION_STRING") 
                ?? builder.Configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

            builder.Services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));

            // Code executor
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500));

            builder.Services.AddHttpClient<CodeExecutorClient>(client =>
            {
                string? url = Environment.GetEnvironmentVariable("CODE_EXECUTOR_URL")
                            ?? builder.Configuration["CodeExecutor:Url"]
                            ?? throw new InvalidOperationException("Code executor url is not configured!");
                client.BaseAddress = new Uri(url);
            })
            .AddPolicyHandler(retryPolicy);
        }

        public static void ConfigureAuthentication(this WebApplicationBuilder builder)
        {
            // JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"] // User Secrets/appsettings
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
                        return Task.CompletedTask;
                    },

                    // Validate security stamp on token validation
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                        {
                            var user = await userManager.FindByIdAsync(userId.ToString());

                            if (user == null)
                            {
                                // User deleted
                                context.Fail("User not found");
                                return;
                            }

                            var securityStampClaim = context.Principal?.FindFirst("security_stamp")?.Value;
                            if (!string.IsNullOrEmpty(securityStampClaim) && securityStampClaim != user.SecurityStamp)
                            {
                                context.Fail("Security stamp has changed. Please login again.");
                                return;
                            }
                        }
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddGoogle(options =>
            {
                var clientId = builder.Configuration["Authentication:Google:ClientId"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

                var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

                if (string.IsNullOrEmpty(clientId))
                {
                    throw new InvalidOperationException(
                        "Google Client ID is not configured. " +
                        "Set it in User Secrets, appsettings, or GOOGLE_CLIENT_ID environment variable.");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException(
                        "Google Client Secret is not configured. " +
                        "Set it in User Secrets, appsettings, or GOOGLE_CLIENT_SECRET environment variable.");
                }

                options.ClientId = clientId;
                options.ClientSecret = clientSecret;

                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.SaveTokens = true;
            });
        }

        public static void AddDbContext(this WebApplicationBuilder builder)
        {
            if (builder.Environment.IsEnvironment("Testing")) return;

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
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
        }

        public static void AddIdentity(this WebApplicationBuilder builder)
        {
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
        }

        public static void AddSwagger(this WebApplicationBuilder builder)
        {
            if (builder.Environment.IsProduction()||
                builder.Environment.IsEnvironment("Testing")) return;

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
                        []
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
        }
    }
}
