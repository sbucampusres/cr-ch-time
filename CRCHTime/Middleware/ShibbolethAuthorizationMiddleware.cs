using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CRCHTime.Services;

namespace CRCHTime.Middleware;

public class ShibbolethAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ShibbolethAuthorizationMiddleware> _logger;

    public ShibbolethAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<ShibbolethAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IStoredProcService storedProcService, IApplicationContextService appContextService)
    {
        try
        {
            // Skip authentication for static files, error pages, and the Unauthorized page
            var path = context.Request.Path.Value?.ToLower() ?? "";

            _logger.LogInformation("ShibbolethAuthorizationMiddleware: Processing path: {Path}", path);

            if (path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/favicon") ||
                path.Contains("unauthorized") ||
                path.Contains("error"))
            {
                _logger.LogInformation("ShibbolethAuthorizationMiddleware: Skipping auth for path: {Path}", path);
                await _next(context);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ShibbolethAuthorizationMiddleware initial checks");
            throw;
        }

        // Check if user is already authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Read NetID from Shibboleth server variables (REMOTE_USER)
        string? netId = null;

        try
        {
            var serverVars = context.Features.Get<Microsoft.AspNetCore.Http.Features.IServerVariablesFeature>();
            if (serverVars != null)
            {
                netId = serverVars["REMOTE_USER"];
                _logger.LogInformation("Found REMOTE_USER in server variables: {NetId}", netId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accessing REMOTE_USER server variable");
        }

        // Fallback to headers if server variables don't work
        if (string.IsNullOrWhiteSpace(netId))
        {
            netId = context.Request.Headers["REMOTE_USER"].FirstOrDefault()
                    ?? context.Request.Headers["HTTP_CN"].FirstOrDefault()
                    ?? context.Request.Headers["cn"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(netId))
        {
            _logger.LogWarning("No NetID found in Shibboleth server variables or headers");
            context.Response.Redirect($"{context.Request.PathBase}/Unauthorized");
            return;
        }

        // Trim and normalize NetID
        netId = netId.Trim().ToLower();

        _logger.LogInformation("Shibboleth NetID detected: {NetId}", netId);

        // Get current application context
        var currentApplication = appContextService.GetCurrentApplication();

        // Look up user via package procedure CRFCCS_AUTH_LOOKUP
        var (found, isTerminated, role) = await storedProcService.AuthLookupAsync(netId, currentApplication);

        if (!found)
        {
            _logger.LogWarning("NetID {NetId} not found in WS_FCSTAFF for application {Application}", netId, currentApplication);
            context.Response.Redirect($"{context.Request.PathBase}/Unauthorized");
            return;
        }

        if (isTerminated)
        {
            _logger.LogWarning("NetID {NetId} is terminated", netId);
            context.Response.Redirect($"{context.Request.PathBase}/Unauthorized");
            return;
        }

        // Determine role - map staff Role to access level
        var accessLevel = MapRoleToAccessLevel(role);

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, netId),
            new Claim(ClaimTypes.NameIdentifier, $"{netId}|{currentApplication}"),
            new Claim(ClaimTypes.Role, accessLevel),
            new Claim("DisplayName", netId),
            new Claim("AccessLevel", accessLevel),
            new Claim("Application", currentApplication)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Set the user principal for the current request
        context.User = claimsPrincipal;

        // Sign in the user to create persistent cookie for future requests
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        _logger.LogInformation("User {NetId} authenticated with role {Role} for application {Application}",
            netId, accessLevel, currentApplication);

        await _next(context);
    }

    /// <summary>
    /// Maps application-specific roles to standard authorization roles.
    /// </summary>
    private static string MapRoleToAccessLevel(string? role)
    {
        return role?.ToLower() switch
        {
            "chadmin"       => "Administrator",
            "chsupervisor"  => "Supervisor",
            "chuser"        => "Operator",
            "administrator" => "Administrator",
            "admin"         => "Administrator",
            "supervisor"    => "Supervisor",
            "operator"      => "Operator",
            "user"          => "Operator",
            "viewer"        => "Viewer",
            _               => "Operator"  // Default for null or unknown roles
        };
    }
}
