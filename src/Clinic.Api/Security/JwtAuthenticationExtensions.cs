using System.Text;
using Clinic.Application.Auth;
using Clinic.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Api.Security;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// JWT bearer authentication: the signature, issuer, audience and expiry of every token are validated with the
    /// same <see cref="JwtOptions"/> the token generator signs with. Failures answer in the plain-text style of the
    /// rest of the API: 401 when there is no valid token, 403 when the role is not allowed.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>((options, jwt) =>
            {
                // Keep the claim names as written in the token ("sub", "role", "branch_id", ...) instead of
                // letting the handler rename them to long .NET claim type URIs.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    // [Authorize(Roles = ...)] reads the "role" claim.
                    RoleClaimType = AuthClaims.Role,
                    NameClaimType = "name"
                };

                options.Events = new JwtBearerEvents
                {
                    // A valid signature is not enough: the account must still exist and be active (one primary-key
                    // lookup per authenticated request). Failing here answers 401, so the frontend ends the session.
                    OnTokenValidated = async context =>
                    {
                        var checker = context.HttpContext.RequestServices.GetRequiredService<IUserStatusChecker>();
                        if (!await TokenUserValidation.IsAccountUsableAsync(context.Principal, checker, context.HttpContext.RequestAborted))
                        {
                            context.Fail("The account no longer exists or has been deactivated.");
                        }
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse(); // replace the empty default response
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await context.Response.WriteAsync(context.AuthenticateFailure is null
                            ? "Authentication is required: log in and send the token as a Bearer token."
                            : "The token is invalid or has expired.");
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return context.Response.WriteAsync("Your role is not allowed to perform this action.");
                    }
                };
            });

        // Safety net: an endpoint that someone forgets to annotate requires a login instead of being public.
        // (Every endpoint is still annotated explicitly; a test checks that.)
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
