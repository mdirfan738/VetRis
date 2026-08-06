using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using VetRIS.Api.Data;
using VetRIS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Configuration: all secrets/config are read from environment variables
// or (in AWS) injected via App Runner / ECS task definition from Secrets
// Manager. Nothing sensitive is hard-coded. See appsettings.json for the
// non-secret defaults and README.md for the full list of env vars.
// ---------------------------------------------------------------------

var dbConnectionString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("DB_CONNECTION_STRING is not configured");

builder.Services.AddDbContext<VetrisDbContext>(options =>
    options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString)));

// AWS SDK picks up credentials automatically from the App Runner /
// ECS task role in AWS, or from the local AWS profile / env vars in dev.
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddScoped<IS3UploadService, S3UploadService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigin = builder.Configuration["ALLOWED_ORIGIN"] ?? "http://localhost:5173";
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In production this app sits behind CloudFront/App Runner, which terminate
// TLS. All traffic between CloudFront and App Runner, and App Runner and
// RDS, should also be encrypted (App Runner is HTTPS-only; RDS uses TLS
// via the require_secure_transport parameter — see terraform/modules/database).
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Auto-migrate on startup for the scaffold. Replace with a proper
// migration step in CI/CD before this goes near production data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VetrisDbContext>();
    db.Database.Migrate();
}

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy"
    });
});

app.Run();
