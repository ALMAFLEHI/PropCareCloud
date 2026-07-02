using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string frontendDevelopmentPolicy = "FrontendDevelopment";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(frontendDevelopmentPolicy);

app.MapControllers();

app.Run();
