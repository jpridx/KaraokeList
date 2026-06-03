# KaraokeList

A Blazor web application for managing karaoke song catalogs, venues, and singer performance records.

## Overview

KaraokeList is a comprehensive karaoke management system built with .NET Blazor and Syncfusion components. It provides a clean, intuitive interface for managing songs, artists, genres, venues, and tracking singer performances.

## Technology Stack

- **Framework**: .NET Blazor (Interactive Server Render Mode)
- **Language**: C#
- **Database**: Azure SQL / SQL Server (serverless on Azure)
- **UI Components**: Syncfusion Blazor with Fluent 2 theme
- **Authentication**: Built-in Identity system
- **Styling**: Bootstrap CSS with custom styling

## Features

### Core Pages

- **Songs**: Manage karaoke song catalog with artist information
- **Artists**: Track and manage artists/performers
- **Genres**: Organize songs by musical genre
- **Singers**: Maintain a roster of singers
- **Venues**: Manage karaoke venues/locations
- **Singer Songs**: Administrative tracking of singer performances (when/where/how often they performed each song)

### Functionality

- Full CRUD operations (Create, Read, Update, Delete) for all entities
- Syncfusion data grids with:
  - Paging and sorting
  - Filtering and searching
  - Inline editing
  - Multi-select capabilities
- Dropdown lookups for related data
- Date pickers for performance tracking
- Numeric fields for performance counts
- User authentication and account management

## Project Structure

```
KaraokeList/
├── Components/
│   ├── Pages/               # Blazor pages
│   │   ├── Songs.razor
│   │   ├── Artists.razor
│   │   ├── Genres.razor
│   │   ├── Singers.razor
│   │   ├── Venues.razor
│   │   ├── SingerSongs.razor
│   │   └── ...
│   ├── Layout/              # App layout components
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── Account/             # Authentication pages
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Services/            # Data access services
│   │   ├── SongService.cs
│   │   ├── ArtistService.cs
│   │   ├── GenreService.cs
│   │   ├── SingerService.cs
│   │   ├── VenueService.cs
│   │   └── SingerSongService.cs
│   └── Models/
├── Temp/
│   └── Karaoke.sqlite3      # SQLite database file
├── docs/                    # Project documentation
│   ├── KaraokeList.md       # Main documentation
│   ├── Artists.md
│   ├── Genres.md
│   ├── Singers.md
│   ├── SingerSongs.md
│   ├── Songs.md
│   └── Venues.md
└── Properties/
    └── launchSettings.json
```

## Getting Started

### Prerequisites

- .NET 9.0 or .NET 10.0 SDK
- Visual Studio or VS Code with C# extensions

### Setup

1. Clone the repository
2. Open `KaraokeList.sln` in Visual Studio
3. Restore NuGet packages
4. Run the application

### Running the App

Using Visual Studio:
1. Set `KaraokeList` as the startup project
2. Press F5 or select Debug → Start Debugging

The app will open at `https://localhost:7000` (or configured HTTPS port).

## Database

Catalog and Identity data share one SQL Server / Azure SQL database via `ConnectionStrings:DefaultConnection`.

### Local development

Default connection (LocalDB):

```
Server=(localdb)\mssqllocaldb;Database=KaraokeList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Catalog tables are created automatically on startup from `scripts/azure-sql/001-karaoke-schema.sql`.

### Azure deployment

See [docs/azure-deployment.md](docs/azure-deployment.md) for App Service + Azure SQL serverless provisioning and publish steps.

### Tables

- **Songs** - Karaoke song catalog
- **Artists** - Song performers/artists
- **Genres** - Musical genres/categories
- **Singers** - Registered singers
- **Venues** - Karaoke venues/locations
- **SingerSongs** - Performance history (singer, song, venue, dates, count)

Refer to the schema documentation in `docs/` for detailed table structures.

## Data Management

### Services

All data access is handled through service classes in `KaraokeList.Data`:

- `SongService` - Song catalog management
- `ArtistService` - Artist management
- `GenreService` - Genre management
- `SingerService` - Singer roster management
- `VenueService` - Venue management
- `SingerSongService` - Performance record tracking

Each service provides async CRUD operations using parameterized SQL queries with SQL Server.

### Adding Data

1. Navigate to the respective management page (Songs, Artists, Genres, Singers, Venues)
2. Click "Add" in the toolbar
3. Fill in the required fields
4. Click "Save"

Lookup relationships are automatically enforced through dropdown selectors.

## Documentation

Detailed documentation for each feature is available in the `docs/` folder:

- `docs/KaraokeList.md` - Complete project documentation and feature overview
- `docs/Artists.md` - Artist table schema
- `docs/Genres.md` - Genre table schema
- `docs/Singers.md` - Singer table schema
- `docs/SingerSongs.md` - Singer song performance table schema
- `docs/Songs.md` - Song table schema
- `docs/Venues.md` - Venue table schema

## Authentication

Friends-only access uses ASP.NET Core Identity with an **invite code**, sign-in lockout, and rate limits. Catalog pages require authentication.

- Share the site URL and invite code privately (see [docs/security-private-access.md](docs/security-private-access.md))
- After your group has accounts, disable new registration in Azure (`Security__Registration__AllowRegistration=false`)
- Deploy and auth setup: [docs/azure-deployment.md](docs/azure-deployment.md)

## Future Enhancements

- User-facing performance logging page for singers to record their own performances
- Advanced reporting and statistics
- Search and filter enhancements
- Export functionality
- Mobile-optimized views

## License

[Add your license information here]

## Support

For issues or questions, please refer to the project documentation in the `docs/` folder or contact the development team.
