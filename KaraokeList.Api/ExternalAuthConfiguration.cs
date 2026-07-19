using KaraokeList.Api.Services;
using KaraokeList.Security;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

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
            });
        }

        if (authSettings.Microsoft.IsConfigured)
        {
            authBuilder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authSettings.Microsoft.ClientId;
                options.ClientSecret = authSettings.Microsoft.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
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
}
