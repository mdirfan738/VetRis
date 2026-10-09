using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using VetRIS.Api.Middleware;
using VetRIS.Api.Services;
using VetRIS.Application;
using VetRIS.Application.Common.Interfaces;
using VetRIS.Domain.Enums;
using VetRIS.Infrastructure;
using VetRIS.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// --- Compatibility bridge for mdirfan738/VetRis's existing scaffold ---
// That repo's docker-compose.yml / Terraform already set these exact env
// var names for a flatter, single-project API. Rather than asking already-
// working infrastructure to change, this solution's own config keys
// (ConnectionStrings:VetRISDb, Cors:AllowedOrigins, S3:BucketName) get
// populated FROM them when present, so both naming conventions work.
var dbConnectionFromEnv = builder.Configuration["DB_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(dbConnectionFromEnv))
{
    builder.Configuration["ConnectionStrings:VetRISDb"] = dbConnectionFromEnv;
}

var allowedOriginFromEnv = builder.Configuration["ALLOWED_ORIGIN"];
if (!string.IsNullOrWhiteSpace(allowedOriginFromEnv))
{
    builder.Configuration["Cors:AllowedOrigins:0"] = allowedOriginFromEnv;
}

var dicomBucketFromEnv = builder.Configuration["Aws:DicomActiveBucket"];
if (!string.IsNullOrWhiteSpace(dicomBucketFromEnv))
{
    builder.Configuration["S3:BucketName"] = dicomBucketFromEnv;
}

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- Application + Infrastructure + background jobs composition (see each layer's own DependencyInjection.cs / BackgroundJobsRegistration.cs) ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBackgroundJobs();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- Global exception handling (.NET 8 IExceptionHandler pipeline) ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- MVC + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "VetRIS API", Version = "v1" });

    // Lets Swagger UI's "Try it out" target either environment directly
    // instead of always the host it's currently being served from.
    options.AddServer(new OpenApiServer { Url = "https://api.alua2kg.online", Description = "Production" });
    options.AddServer(new OpenApiServer { Url = "http://localhost:8080", Description = "Local / Docker Compose" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste a JWT from POST /api/auth/login (no \"Bearer \" prefix needed here)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// --- JWT bearer auth ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it via user-secrets/environment/Secrets Manager — never commit a real key to appsettings.json.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();

// --- CORS: the React/Vite app is a different origin, always ---
const string CorsPolicy = "VetRISFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Two paths, one handler: /health is this solution's own convention;
// /api/health matches what mdirfan738/VetRis's Dockerfile HEALTHCHECK and
// (likely) its ALB target group already probe. Both anonymous.
var healthCheck = () => Results.Ok(new { status = "healthy", timestampUtc = DateTime.UtcNow });
app.MapGet("/health", healthCheck).AllowAnonymous();
app.MapGet("/api/health", healthCheck).AllowAnonymous();

await SeedRolesAsync(app);
await SeedInitialAdminAsync(app);

app.Run();

// Seeds the fixed role set from Domain's UserRoleType enum. Idempotent —
// safe to run on every startup. User accounts and role *assignment* are
// provisioned by an admin until the Authentication & Authorization phase
// adds a proper registration/onboarding flow.
static async Task SeedRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    foreach (var roleName in Enum.GetNames<UserRoleType>())
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new ApplicationRole(roleName));
        }
    }
}

/// <summary>
/// Solves the chicken-and-egg problem noted in API/README.md: creating a
/// staff account normally needs an authenticated SuperAdmin (see
/// UsersController), but the very first one doesn't exist yet. Runs on
/// every startup but only ever acts once — if a SuperAdmin already exists,
/// or if VETRIS_ADMIN_USERNAME/VETRIS_ADMIN_PASSWORD aren't both set, it's
/// a no-op. The password is never read from, or written to, any committed
/// file — only from an environment variable you set via GitHub
/// Secrets/your deploy pipeline/a gitignored local .env (see .env.example
/// at the repo root). This is what actually creates the "Admin" account
/// referenced in README.md's Access &amp; Credentials section.
/// </summary>
static async Task SeedInitialAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var existingSuperAdmins = await userManager.GetUsersInRoleAsync(nameof(UserRoleType.SuperAdmin));
    if (existingSuperAdmins.Count > 0)
    {
        return;
    }

    var adminUsername = Environment.GetEnvironmentVariable("VETRIS_ADMIN_USERNAME");
    var adminPassword = Environment.GetEnvironmentVariable("VETRIS_ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
    {
        return;
    }

    var adminUser = new ApplicationUser
    {
        UserName = adminUsername,
        Email = $"{adminUsername.ToLowerInvariant()}@alua2kg.online",
        FirstName = "System",
        LastName = "Administrator",
        EmailConfirmed = true,
        IsActive = true
    };

    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, nameof(UserRoleType.SuperAdmin));
    }
    else
    {
        app.Logger.LogError("Initial admin seeding failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

/// <summary>Exposed so WebApplicationFactory-based integration tests (added in the Testing phase) can bootstrap against this same composition root.</summary>
public partial class Program
{
}
