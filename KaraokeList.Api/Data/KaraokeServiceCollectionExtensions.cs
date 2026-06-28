namespace KaraokeList.Data;

public static class KaraokeServiceCollectionExtensions
{
    public static IServiceCollection AddKaraokeDataServices(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(_ => new VenueService(connectionString));
        services.AddScoped(_ => new SongService(connectionString));
        services.AddScoped(_ => new ArtistService(connectionString));
        services.AddScoped(_ => new GenreService(connectionString));
        services.AddScoped(_ => new SingerService(connectionString));
        services.AddScoped(_ => new PerformanceService(connectionString));
        services.AddScoped(_ => new ArtistLookupService(connectionString));
        services.AddScoped(_ => new CatalogIntegrityService(connectionString));
        services.AddScoped(sp => new SingerListService(
            sp.GetRequiredService<ApplicationDbContext>(),
            sp.GetRequiredService<CatalogIntegrityService>(),
            connectionString));
        services.AddScoped<TicklerExclusionService>();
        return services;
    }
}
