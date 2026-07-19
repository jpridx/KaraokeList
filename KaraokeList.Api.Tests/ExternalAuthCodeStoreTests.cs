using KaraokeList.Api.Services;

namespace KaraokeList.Api.Tests;

public sealed class ExternalAuthCodeStoreTests
{
    [Fact]
    public void ConsumeCode_returns_null_for_unknown_code()
    {
        var store = new ExternalAuthCodeStore();
        Assert.Null(store.ConsumeCode("not-a-code"));
    }

    [Fact]
    public void ConsumeCode_returns_entry_once()
    {
        var store = new ExternalAuthCodeStore();
        var code = store.CreateCode("user-1", rememberMe: true);

        var first = store.ConsumeCode(code);
        var second = store.ConsumeCode(code);

        Assert.NotNull(first);
        Assert.Equal("user-1", first!.UserId);
        Assert.True(first.RememberMe);
        Assert.Null(second);
    }
}
