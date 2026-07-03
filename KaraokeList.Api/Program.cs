// KaraokeList.Api — JSON API only (no Syncfusion; see KaraokeList.Web for UI).
using System.Text;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// Required by ExcelDataReader for reading legacy .xls files
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = aiConnectionString);
}

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddKaraokeDataServices(connectionString);
builder.Services.Configure<RegistrationSettings>(builder.Configuration.GetSection(RegistrationSettings.SectionName));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<IRegistrationGate, RegistrationGate>();
builder.Services.AddSingleton<IAccountEmailSender, AccountEmailSender>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAuthRateLimiter, AuthRateLimiter>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICurrentUserSingerResolver, CurrentUserSingerResolver>();
builder.Services.AddScoped<IAiGenreService, AiGenreService>();

builder.Services.AddHttpClient("GoogleSheets", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("KaraokeList/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Key) && settings.Key.Length >= 32,
        "Jwt:Key must be at least 32 characters.")
    .ValidateOnStart();

var requireConfirmedAccount = builder.Configuration.GetValue("Identity:RequireConfirmedAccount", false);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwt) =>
    {
        var settings = jwt.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key))
        };
    });

builder.Services.AddAuthorization();

var webOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5262", "https://localhost:7262"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(webOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Azure App Service, Cloudflare, and other reverse proxies terminate TLS and forward HTTP
// to Kestrel with X-Forwarded-Proto. Without this, Request.IsHttps stays false.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(KaraokeList.Shared.KaraokeRoles.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(KaraokeList.Shared.KaraokeRoles.Admin));
    }
}

app.UseForwardedHeaders();

app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/version", async (ApplicationDbContext db) =>
{
    var migrations = await db.Database.GetAppliedMigrationsAsync();
    var latestMigration = migrations.LastOrDefault() ?? "none";
    var songCount = await db.Songs.CountAsync();
    var maxSongId = songCount > 0 ? await db.Songs.MaxAsync(s => s.Id) : 0;
    return Results.Ok(new KaraokeList.Shared.AppVersionDto
    {
        CacheTag = $"{latestMigration}:{songCount}:{maxSongId}"
    });
}).RequireCors("WebClient");

app.Run();
