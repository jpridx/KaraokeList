using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class TicklerExclusionValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeReason_returns_null_for_blank_values(string? reason)
    {
        Assert.Null(TicklerExclusionValidation.NormalizeReason(reason));
    }

    [Fact]
    public void NormalizeReason_trims_value()
    {
        Assert.Equal("too hard", TicklerExclusionValidation.NormalizeReason("  too hard  "));
    }

    [Fact]
    public void ValidateReason_rejects_long_values()
    {
        var error = TicklerExclusionValidation.ValidateReason(new string('x', 26));

        Assert.Equal("Reason must be 25 characters or fewer.", error);
    }

    [Fact]
    public void ValidateReason_accepts_short_values()
    {
        Assert.Null(TicklerExclusionValidation.ValidateReason("out of range"));
    }
}
