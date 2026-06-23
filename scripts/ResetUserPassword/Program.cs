using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResetUserPassword;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project scripts/ResetUserPassword -- <email> <new-password>");
    Console.Error.WriteLine("Connection: KARAOKE_SQL_CONNECTION, or LocalDB KaraokeList (dev default).");
    return 1;
}

var email = args[0];
var newPassword = args[1];

var connectionString = Environment.GetEnvironmentVariable("KARAOKE_SQL_CONNECTION")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=KaraokeList;Trusted_Connection=True;TrustServerCertificate=True";

var services = new ServiceCollection();
services.AddDbContext<ResetDbContext>(options => options.UseSqlServer(connectionString));
services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<ResetDbContext>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

var user = await userManager.FindByEmailAsync(email);
if (user is null)
{
    Console.Error.WriteLine($"No user found for {email}.");
    return 1;
}

await userManager.SetLockoutEndDateAsync(user, null);
await userManager.ResetAccessFailedCountAsync(user);

var hasher = new PasswordHasher<ApplicationUser>();
user.PasswordHash = hasher.HashPassword(user, newPassword);
var result = await userManager.UpdateAsync(user);
if (!result.Succeeded)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
    return 1;
}

Console.WriteLine($"Password reset for {email}. Sign in with the new password.");
return 0;
