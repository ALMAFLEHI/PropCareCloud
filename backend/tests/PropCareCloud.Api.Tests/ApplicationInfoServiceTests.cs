using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class ApplicationInfoServiceTests
{
    private readonly ApplicationInfoService _service = new();

    [Fact]
    public void GetApplicationInfo_ReturnsExpectedApplicationName()
    {
        var result = _service.GetApplicationInfo();

        Assert.Equal("PropCare Cloud", result.ApplicationName);
    }

    [Fact]
    public void GetApplicationInfo_ReturnsExpectedModule()
    {
        var result = _service.GetApplicationInfo();

        Assert.Equal("CT071-3-3-DDAC", result.Module);
    }

    [Fact]
    public void GetApplicationInfo_ReturnsArchitectureWithWebApi()
    {
        var result = _service.GetApplicationInfo();

        Assert.Contains("ASP.NET Core Web API", result.Architecture);
    }

    [Fact]
    public void GetApplicationInfo_ReturnsNonEmptyValues()
    {
        var result = _service.GetApplicationInfo();

        Assert.False(string.IsNullOrWhiteSpace(result.ApplicationName));
        Assert.False(string.IsNullOrWhiteSpace(result.Module));
        Assert.False(string.IsNullOrWhiteSpace(result.Architecture));
        Assert.False(string.IsNullOrWhiteSpace(result.Environment));
    }
}
