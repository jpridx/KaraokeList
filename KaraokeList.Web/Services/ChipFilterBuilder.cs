using KaraokeList.Web.Components;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Services;

public static class ChipFilterBuilder
{
    public static IReadOnlyList<ChipFilterItem> CreateAllPlusItems<TItem>(
        int? activeId,
        IEnumerable<TItem> items,
        Func<TItem, int> getId,
        Func<TItem, string> getLabel,
        EventCallbackFactory factory,
        object receiver,
        Func<int?, Task> onSelectAsync)
    {
        var chips = new List<ChipFilterItem>
        {
            new()
            {
                Label = "All",
                IsActive = activeId is null,
                OnClick = factory.Create(receiver, () => onSelectAsync(null))
            }
        };

        chips.AddRange(items.Select(item => new ChipFilterItem
        {
            Label = getLabel(item),
            IsActive = activeId == getId(item),
            OnClick = factory.Create(receiver, () => onSelectAsync(getId(item)))
        }));

        return chips;
    }
}
