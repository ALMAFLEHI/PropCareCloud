# PropCareCloud.Api Backend

PropCareCloud.Api is the ASP.NET Core Web API backend foundation for PropCare Cloud.

## Technology

- ASP.NET Core Web API
- .NET 8
- Controllers
- Swagger/OpenAPI in Development
- xUnit test project

## Restore

```powershell
dotnet restore .\backend\PropCareCloud.sln
```

## Build

```powershell
dotnet build .\backend\PropCareCloud.sln
```

## Run

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

## Test

```powershell
dotnet test .\backend\PropCareCloud.sln
```

## Current Endpoints

- `GET /api/health`
- `GET /api/system-info`

## Notes

Database access, authentication, authorization rules, and AWS deployment will be added in later sprints. No production secrets, AWS credentials, or database connection strings are configured in this sprint.
