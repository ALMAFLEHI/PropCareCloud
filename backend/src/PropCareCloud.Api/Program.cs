using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropCareCloud.Api.Configuration;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string frontendDevelopmentPolicy = "FrontendDevelopment";
const string developmentOnlyJwtSigningKey =
    "DevelopmentOnlyJwtSigningKeyForLocalDemo_DoNotUseInProduction";
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("PROPCLOUD_CONNECTION_STRING");
}

var isDatabaseConfigured = !string.IsNullOrWhiteSpace(connectionString);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PropCareCloud";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PropCareCloud.Frontend";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
var configuredAllowedOrigins = (Environment.GetEnvironmentVariable("PROPCLOUD_ALLOWED_ORIGINS") ?? string.Empty)
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var allowedOrigins = new[] { "http://localhost:5173" }
    .Concat(configuredAllowedOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    // Development-only fallback for the local assignment demo. Do not use in production.
    jwtSigningKey = developmentOnlyJwtSigningKey;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter a JWT bearer token returned from /api/auth/login."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(nameof(UserRole.AdminOwner)));
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireRole(nameof(UserRole.AdminOwner), nameof(UserRole.PropertyManager)));
    options.AddPolicy("AdminManagerOrStaff", policy =>
        policy.RequireRole(
            nameof(UserRole.AdminOwner),
            nameof(UserRole.PropertyManager),
            nameof(UserRole.MaintenanceStaff)));
    options.AddPolicy("AllRoles", policy =>
        policy.RequireRole(
            nameof(UserRole.AdminOwner),
            nameof(UserRole.PropertyManager),
            nameof(UserRole.Tenant),
            nameof(UserRole.MaintenanceStaff)));
    options.AddPolicy("AllPortalRoles", policy =>
        policy.RequireRole(
            nameof(UserRole.AdminOwner),
            nameof(UserRole.PropertyManager),
            nameof(UserRole.Tenant),
            nameof(UserRole.MaintenanceStaff)));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<Task2AttachmentOptions>(
    builder.Configuration.GetSection(Task2AttachmentOptions.SectionName));
builder.Services.AddHttpClient<ITask2AttachmentGateway, Task2AttachmentGatewayClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
if (isDatabaseConfigured)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddScoped<ISeedDataService, SeedDataService>();
    builder.Services.AddScoped<IPropertyService, PropertyService>();
    builder.Services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
    builder.Services.AddScoped<IMaintenanceAttachmentService, MaintenanceAttachmentService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<ITenantRegistrationService, TenantRegistrationService>();
}
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendDevelopmentPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
builder.Services.AddSingleton<IDomainSummaryService, DomainSummaryService>();
builder.Services.AddSingleton<IDatabaseStatusService, DatabaseStatusService>();
builder.Services.AddScoped<IDatabaseReadinessService, DatabaseReadinessService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(frontendDevelopmentPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
