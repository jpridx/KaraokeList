# KaraokeList

A Blazor web application for managing karaoke song catalogs, venues, and singer performance records.

## Overview

KaraokeList is a comprehensive karaoke management system built with .NET Blazor and Syncfusion components. It provides a clean, intuitive interface for managing songs, artists, genres, venues, and tracking singer performances.

## Technology Stack

- **Framework**: .NET Blazor (Interactive Server Render Mode)
- **Language**: C#
- **Database**: SQLite
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

The application uses SQLite for data persistence. The database file is located at `Temp/Karaoke.sqlite3`.

### Connection String

The connection string is built dynamically:
```
Data Source={AppDomain.CurrentDomain.BaseDirectory}Temp/Karaoke.sqlite3
```

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

Each service provides async CRUD operations using parameterized SQL queries with SQLite.

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

The application includes built-in user authentication:

- User registration and login
- Account management
- Secure password handling
- Identity integration with Entity Framework Core

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
