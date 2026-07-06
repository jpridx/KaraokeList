using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class ChangePasswordPageTests : AuthPageTestContext
{
    [Fact]
    public void Renders_change_password_form()
    {
        var cut = Render<ChangePassword>();

        Assert.Contains("Change password", cut.Markup);
        cut.Find("button[type=submit]");
    }

    [Fact]
    public void Shows_success_when_password_changes()
    {
        Api.Setup(client => client.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>()))
            .ReturnsAsync(ChangePasswordResult.Ok());

        var cut = Render<ChangePassword>();
        SubmitForm(cut, "OldPassw0rd!23", "NewPassw0rd!99", "NewPassw0rd!99");

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-success");
            Assert.Contains("password has been changed", alert.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Shows_error_when_change_fails()
    {
        Api.Setup(client => client.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>()))
            .ReturnsAsync(ChangePasswordResult.Fail("Current password is incorrect."));

        var cut = Render<ChangePassword>();
        SubmitForm(cut, "wrong", "NewPassw0rd!99", "NewPassw0rd!99");

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger");
            Assert.Contains("Current password is incorrect.", alert.TextContent);
        });
    }

    private static void SubmitForm(
        IRenderedComponent<ChangePassword> cut,
        string current,
        string newPassword,
        string confirm)
    {
        var inputs = cut.FindAll("input.form-control");
        inputs[0].Change(current);
        inputs = cut.FindAll("input.form-control");
        inputs[1].Change(newPassword);
        inputs = cut.FindAll("input.form-control");
        inputs[2].Change(confirm);
        cut.Find("button[type=submit]").Click();
    }
}
