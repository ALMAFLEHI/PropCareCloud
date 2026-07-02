namespace PropCareCloud.Api.Models;

public sealed record ApplicationInfo(
    string ApplicationName,
    string Module,
    string Architecture,
    string Environment);
