# KaraokeList

## Project overview

`KaraokeList` is a Blazor web application built with Syncfusion Blazor components. It includes a simple karaoke song and artist management interface backed by a local SQLite database, plus several example pages that demonstrate Syncfusion controls.

> This file is to be included during any request concerning this project and updated as needed.

The app uses a temporary SQLite file located under `Temp/Karaoke.sqlite3` and provides both data management pages and UI component demo pages.

## Main pages

### Home / Index
- File: `Components/Pages/Index.razor`
- Route: `/`
- Purpose: landing page and navigation hub.
- Shows grouped links for Syncfusion component demos and the karaoke data pages.

### Songs
- File: `Components/Pages/Songs.razor`
- Route: `/songs`
- Purpose: manage karaoke songs.
- Displays a Syncfusion data grid with song records.
- Supports paging, sorting, filtering, selection, inline add/edit/delete, and search.
- Uses `SongService` and `ArtistLookupService` to load songs and artist names.
- Provides an autocomplete editor for selecting the song artist.

### Artists
- File: `Components/Pages/Artists.razor`
- Route: `/artists`
- Purpose: manage karaoke artists.
- Displays a Syncfusion data grid with artist records.
- Supports paging, sorting, filtering, grouping, resizing, selection, and inline add/edit/delete.
- Uses `ArtistService` to load, add, update, and delete artists.

### Auth
- File: `Components/Pages/Auth.razor`
- Route: `/auth`
- Purpose: authenticated user page.
- Requires login via `[Authorize]`.
- Displays a greeting with the authenticated user name.

### Error
- File: `Components/Pages/Error.razor`
- Route: `/Error`
- Purpose: runtime error page.
- Shows a friendly error message and request ID.
- Includes guidance about using the `Development` environment for detailed debugging.

## Syncfusion component demo pages

These pages demonstrate individual Syncfusion Blazor controls with sample data and configurations.

### AutoComplete
- File: `Components/Pages/AutoCompleteFeatures.razor`
- Route: `/autocomplete-features`
- Demo: `SfAutoComplete` with a searchable game list.

### ComboBox
- File: `Components/Pages/ComboBoxFeatures.razor`
- Route: `/combobox-features`
- Demo: `SfComboBox` with a dropdown game selector.

### Dropdown List
- File: `Components/Pages/DropdownListFeatures.razor`
- Route: `/dropdownlist-features`
- Demo: `SfDropDownList` with a game selection list.

### DataGrid
- File: `Components/Pages/DataGridFeatures.razor`
- Route: `/datagrid-features`
- Demo: `SfGrid` with example order data.
- Shows filtering, grouping, sorting, selection, paging, resizing, editing, and Excel export.

### DatePicker
- File: `Components/Pages/DatePickerFeatures.razor`
- Route: `/datepicker-features`
- Demo: `SfDatePicker` with date range limits and input masking.

### Checkbox
- File: `Components/Pages/CheckboxFeatures.razor`
- Route: `/checkbox-features`
- Demo: `SfCheckBox` in enabled, disabled, and indeterminate states.

### Radio Button
- File: `Components/Pages/RadioButtonFeatures.razor`
- Route: `/radiobutton-features`
- Demo: `SfRadioButton` set for payment method selection.

### Rating
- File: `Components/Pages/RatingFeatures.razor`
- Route: `/rating-features`
- Demo: `SfRating` with custom icons, tooltip, label, reset, and selection.

### TextBox
- File: `Components/Pages/TextBoxFeatures.razor`
- Route: `/textbox-features`
- Demo: `SfTextBox` fields for first name and last name.

### Weather
- File: `Components/Pages/Weather.razor`
- Route: `/weather`
- Demo: streaming rendering of generated weather forecast data.

## Notes

- Data services are implemented under `KaraokeList/Data`.
- For schema-level reference, see the data docs in the `docs` folder: `Artists.md`, `Genres.md`, `Singers.md`, `SingerSongs.md`, `Songs.md`, and `Venues.md`.
- The app combines demo pages with real karaoke song and artist management functionality.
- This documentation file is intended as the main overview for the project.
