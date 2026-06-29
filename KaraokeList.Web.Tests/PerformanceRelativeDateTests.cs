using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class PerformanceRelativeDateTests
{
    [Fact]
    public void DaysSince_uses_calendar_dates_not_time_of_day()
    {
        var today = new DateTime(2026, 6, 28);
        var last = new DateTime(2026, 6, 27, 23, 59, 0);

        Assert.Equal(1, PerformanceRelativeDate.DaysSince(last, today));
    }

    [Fact]
    public void DaysSince_returns_null_when_no_last_performance()
    {
        Assert.Null(PerformanceRelativeDate.DaysSince(null, DateTime.Today));
    }

    [Theory]
    [InlineData(0, "today")]
    [InlineData(1, "1 day ago")]
    [InlineData(2, "2 days ago")]
    [InlineData(120, "120 days ago")]
    public void FormatDaysSince_uses_expected_phrasing(int days, string expected)
    {
        Assert.Equal(expected, PerformanceRelativeDate.FormatDaysSince(days));
    }

    [Fact]
    public void StaleCutoff_subtracts_days_from_calendar_date()
    {
        var today = new DateTime(2026, 6, 28);

        Assert.Equal(new DateTime(2026, 3, 30), PerformanceRelativeDate.StaleCutoff(90, today));
    }

    [Fact]
    public void TryParseAsOfDate_parses_iso_date_and_defaults_when_empty()
    {
        Assert.True(PerformanceRelativeDate.TryParseAsOfDate("2026-06-28", out var parsed));
        Assert.Equal(new DateTime(2026, 6, 28), parsed);

        Assert.True(PerformanceRelativeDate.TryParseAsOfDate(null, out var defaulted));
        Assert.Equal(DateTime.Today, defaulted.Date);
    }

    [Fact]
    public void TryParseAsOfDate_rejects_invalid_value()
    {
        Assert.False(PerformanceRelativeDate.TryParseAsOfDate("not-a-date", out _));
    }

    [Fact]
    public void ToQueryDate_uses_iso_format()
    {
        Assert.Equal("2026-06-28", PerformanceRelativeDate.ToQueryDate(new DateTime(2026, 6, 28)));
    }

    [Fact]
    public void FormatLastSang_uses_yesterday_for_one_day_ago()
    {
        var today = new DateTime(2026, 6, 28);
        var last = new DateTime(2026, 6, 27);

        var text = PerformanceRelativeDate.FormatLastSang(last, today, "Silver Sevens");

        Assert.Equal("yesterday at Silver Sevens (6/27/2026)", text);
    }
}
