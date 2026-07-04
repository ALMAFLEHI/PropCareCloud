using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.DTOs.Auth;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IServiceProvider serviceProvider) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var authService = serviceProvider.GetService<IAuthService>();
        if (authService is null)
        {
            return BadRequest(new LoginResponse(
                Success: false,
                Message: "Database connection is not configured. Configure local PostgreSQL before logging in.",
                Token: null,
                ExpiresAtUtc: null,
                User: null));
        }

        var response = await authService.LoginAsync(request);

        return response.Success ? Ok(response) : Unauthorized(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> Me()
    {
        var authService = serviceProvider.GetService<IAuthService>();
        if (authService is null)
        {
            return BadRequest(new { message = "Database connection is not configured." });
        }

        var userProfileIdClaim = User.FindFirstValue("userProfileId") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userProfileIdClaim, out var userProfileId))
        {
            return Unauthorized(new { message = "User profile claim was not found." });
        }

        var user = await authService.GetCurrentUserAsync(userProfileId);
        if (user is null)
        {
            return NotFound(new { message = "Authenticated user profile was not found." });
        }

        return Ok(user);
    }

    [HttpGet("demo-credentials")]
    public async Task<ActionResult<List<DemoCredentialResponse>>> DemoCredentials()
    {
        var authService = serviceProvider.GetService<IAuthService>();
        if (authService is null)
        {
            return Ok(new List<DemoCredentialResponse>
            {
                new("Admin / Owner", "admin@propcare.demo", "PropCare@Admin123", "Demo assignment account."),
                new("Property Manager", "manager@propcare.demo", "PropCare@Manager123", "Demo assignment account."),
                new("Tenant", "tenant@propcare.demo", "PropCare@Tenant123", "Demo assignment account."),
                new("Maintenance Staff", "staff@propcare.demo", "PropCare@Staff123", "Demo assignment account.")
            });
        }

        return Ok(await authService.GetDemoCredentialsAsync());
    }

    /// <summary>
    /// Creates local assignment demo accounts. This endpoint is not intended for production use.
    /// </summary>
    [HttpPost("ensure-demo-accounts")]
    public async Task<IActionResult> EnsureDemoAccounts()
    {
        var authService = serviceProvider.GetService<IAuthService>();
        if (authService is null)
        {
            return BadRequest(new
            {
                message = "Database connection is not configured. Configure local PostgreSQL before creating demo accounts."
            });
        }

        await authService.EnsureDemoAccountsAsync();

        return Ok(new
        {
            success = true,
            message = "Demo authentication accounts are ready for local assignment testing."
        });
    }
}
