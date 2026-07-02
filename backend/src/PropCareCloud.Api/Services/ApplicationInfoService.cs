using PropCareCloud.Api.Models;

namespace PropCareCloud.Api.Services;

public interface IApplicationInfoService
{
    ApplicationInfo GetApplicationInfo();
}

public sealed class ApplicationInfoService : IApplicationInfoService
{
    public ApplicationInfo GetApplicationInfo()
    {
        return new ApplicationInfo(
            ApplicationName: "PropCare Cloud",
            Module: "CT071-3-3-DDAC",
            Architecture: "React frontend + ASP.NET Core Web API + Amazon RDS PostgreSQL",
            Environment: "Development");
    }
}
