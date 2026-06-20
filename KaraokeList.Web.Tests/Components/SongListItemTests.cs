using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongListItemTests : TestContext
{
    [Fact]
    public void Renders_title_and_artist()
    {
        var song = CreateSong(performanceCount: 0);

        var cut = Render(song);

        Assert.Contains("Footloose", cut.Markup);
        Assert.Contains("Kenny Loggins", cut.Markup);
    }

    [Fact]
    public void Renders_not_logged_label_when_never_performed()
    {
        var cut = Render(CreateSong(performanceCount: 0));

        var label = cut.Find(".song-list-item-unlogged-label");
        Assert.Equal("Not logged", label.TextContent);
        cut.Find(".song-list-item").ClassList.Contains("song-list-item-unlogged");
    }

    [Fact]
    public void Renders_last_date_and_count_when_performed_before()
    {
        var lastOn = new DateTime(2026, 6, 15);
        var cut = Render(CreateSong(performanceCount: 3, lastPerformedOn: lastOn));

        Assert.Contains("6/15/2026", cut.Find(".song-list-item-date").TextContent);
        Assert.Equal("3×", cut.Find(".song-list-item-count").TextContent);
        Assert.DoesNotContain("song-list-item-unlogged-label", cut.Markup);
    }

    [Fact]
    public void Renders_genre_when_present()
    {
        var cut = Render(CreateSong(genreName: "Pop"));

        Assert.Equal("Pop", cut.Find(".song-list-item-genre").TextContent);
    }

    [Fact]
    public async Task Invokes_OnLog_when_log_button_clicked()
    {
        var song = CreateSong();
        RepertoireSongDto? loggedSong = null;

        var cut = Render(song, onLog: s => loggedSong = s);
        cut.Find(".song-list-item-log").Click();

        Assert.NotNull(loggedSong);
        Assert.Equal(song.SongId, loggedSong.SongId);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Invokes_OnHistory_when_row_clicked()
    {
        var song = CreateSong();
        RepertoireSongDto? selectedSong = null;

        var cut = Render(song, onHistory: s => selectedSong = s);
        cut.Find(".song-list-item-body").Click();

        Assert.NotNull(selectedSong);
        Assert.Equal(song.SongId, selectedSong.SongId);
        await Task.CompletedTask;
    }

    private static RepertoireSongDto CreateSong(
        int performanceCount = 2,
        DateTime? lastPerformedOn = null,
        string genreName = "Rock")
    {
        return new RepertoireSongDto
        {
            SongId = 42,
            Title = "Footloose",
            ArtistName = "Kenny Loggins",
            GenreName = genreName,
            PerformanceCount = performanceCount,
            LastPerformedOn = lastPerformedOn ?? new DateTime(2026, 6, 15)
        };
    }

    private IRenderedComponent<SongListItem> Render(
        RepertoireSongDto song,
        Action<RepertoireSongDto>? onHistory = null,
        Action<RepertoireSongDto>? onLog = null)
    {
        return RenderComponent<SongListItem>(parameters => parameters
            .Add(p => p.Song, song)
            .Add(p => p.OnHistory, EventCallback.Factory.Create<RepertoireSongDto>(this, onHistory ?? (_ => { })))
            .Add(p => p.OnLog, EventCallback.Factory.Create<RepertoireSongDto>(this, onLog ?? (_ => { }))));
    }
}
