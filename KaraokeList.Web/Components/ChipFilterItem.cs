using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Components;

public sealed class ChipFilterItem
{
    public required string Label { get; init; }

    public bool IsActive { get; init; }

    public required EventCallback OnClick { get; init; }
}
