using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class CoPerformerValidationTests
{
    [Fact]
    public void ValidateInputs_accepts_registered_singer_and_guest()
    {
        var error = CoPerformerValidation.ValidateInputs(
            [
                new CoPerformerInputDto { SingerId = 2 },
                new CoPerformerInputDto { DisplayName = "Guest Singer" }
            ],
            primarySingerId: 1,
            _ => true);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateInputs_rejects_primary_singer_as_co_performer()
    {
        var error = CoPerformerValidation.ValidateInputs(
            [new CoPerformerInputDto { SingerId = 1 }],
            primarySingerId: 1,
            _ => true);

        Assert.Equal("You are already logged as the primary performer.", error);
    }

    [Fact]
    public void ValidateInputs_rejects_both_singer_and_guest_name()
    {
        var error = CoPerformerValidation.ValidateInputs(
            [new CoPerformerInputDto { SingerId = 2, DisplayName = "Also Guest" }],
            primarySingerId: 1,
            _ => true);

        Assert.Equal("Each co-performer must be either a registered singer or a guest name, not both.", error);
    }
}
