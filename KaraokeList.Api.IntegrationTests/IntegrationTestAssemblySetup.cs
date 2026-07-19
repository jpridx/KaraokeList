using System.Runtime.CompilerServices;
using System.Text;

namespace KaraokeList.Api.IntegrationTests;

internal static class IntegrationTestAssemblySetup
{
    [ModuleInitializer]
    public static void RegisterCodePagesEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
