using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string frontendDevelopmentPolicy = "FrontendDevelopment";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isDatabaseConfigured = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (isDatabaseConfigured)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddScoped<ISeedDataService, SeedDataService>();
}
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendDevelopmentPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
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

app.MapControllers();

app.Run();
