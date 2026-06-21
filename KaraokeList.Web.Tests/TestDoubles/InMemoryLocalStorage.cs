using Blazored.LocalStorage;
using System.Text.Json;

namespace KaraokeList.Web.Tests.TestDoubles;

internal sealed class InMemoryLocalStorage : ILocalStorageService
{
    private readonly Dictionary<string, string?> _items = new(StringComparer.Ordinal);

    public event EventHandler<ChangingEventArgs>? Changing;
    public event EventHandler<ChangedEventArgs>? Changed;

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _items.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_items.ContainsKey(key));

    public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(key, out var json) || json is null)
        {
            return ValueTask.FromResult(default(T));
        }

        return ValueTask.FromResult(JsonSerializer.Deserialize<T>(json));
    }

    public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_items.GetValueOrDefault(key));

    public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_items.Keys.ElementAtOrDefault(index));

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IEnumerable<string>>(_items.Keys.ToList());

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_items.Count);

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        _items.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            _items.Remove(key);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        _items[key] = JsonSerializer.Serialize(data);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        _items[key] = data;
        return ValueTask.CompletedTask;
    }
}
