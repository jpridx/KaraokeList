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
    public void FormatLastSang_uses_yesterday_for_one_day_ago()
    {
        var today = new DateTime(2026, 6, 28);
        var last = new DateTime(2026, 6, 27);

        var text = PerformanceRelativeDate.FormatLastSang(last, today, "Silver Sevens");

        Assert.Equal("yesterday at Silver Sevens (6/27/2026)", text);
    }
}
