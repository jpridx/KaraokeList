using KaraokeList.Api.Services;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace KaraokeList.Api;

internal static class ExternalAuthConfiguration
{
    public static IServiceCollection AddKaraokeExternalAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthenticationSettings>(configuration.GetSection(AuthenticationSettings.SectionName));
        services.AddSingleton<IExternalAuthCodeStore, ExternalAuthCodeStore>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();

        var authSettings = configuration.GetSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>()
            ?? new AuthenticationSettings();

        var webBaseUrl = configuration.GetValue<string>("App:WebBaseUrl") ?? "http://localhost:5262";

        var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer()
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "KaraokeList.ExternalAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        if (authSettings.Google.IsConfigured)
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authSettings.Google.ClientId;
                options.ClientSecret = authSettings.Google.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Scope.Add("email");
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
                options.Events = new OAuthEvents
                {
                    OnRemoteFailure = context => HandleRemoteFailure(context, webBaseUrl)
                };
            });
        }

        if (authSettings.Microsoft.IsConfigured)
        {
            // OpenID Connect avoids a Microsoft Graph /me call (which often 500s when Graph
            // User.Read is missing or personal-account consent fails). Same /signin-microsoft callback.
            authBuilder.AddOpenIdConnect(ExternalAuthProviders.MicrosoftScheme, options =>
            {
                options.Authority = "https://login.microsoftonline.com/common/v2.0";
                options.ClientId = authSettings.Microsoft.ClientId;
                options.ClientSecret = authSettings.Microsoft.ClientSecret;
                options.CallbackPath = "/signin-microsoft";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = context => HandleRemoteFailure(context, webBaseUrl)
                };
            });
        }

        return services;
    }

    public static ExternalAuthProvidersDto GetProvidersDto(IOptions<AuthenticationSettings> options)
    {
        var settings = options.Value;
        return new ExternalAuthProvidersDto
        {
            GoogleEnabled = settings.Google.IsConfigured,
            MicrosoftEnabled = settings.Microsoft.IsConfigured
        };
    }

    private static Task HandleRemoteFailure(RemoteFailureContext context, string webBaseUrl)
    {
        var message = context.Failure?.Message ?? "External sign-in failed.";
        context.Response.Redirect(BuildOAuthErrorRedirect(webBaseUrl, message));
        context.HandleResponse();
        return Task.CompletedTask;
    }

    private static string BuildOAuthErrorRedirect(string webBaseUrl, string? message) =>
        $"{webBaseUrl.TrimEnd('/')}/auth/callback?error={Uri.EscapeDataString(message ?? "External sign-in failed.")}";
}
