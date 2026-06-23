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
        foreach (var key in _items.Keys.ToList())
        {
            RemoveString(key);
        }

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
        RemoveString(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            RemoveString(key);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        SetString(key, JsonSerializer.Serialize(data));
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        SetString(key, data);
        return ValueTask.CompletedTask;
    }

    private void SetString(string key, string? value)
    {
        _items.TryGetValue(key, out var oldValue);
        if (value is null)
        {
            _items.Remove(key);
        }
        else
        {
            _items[key] = value;
        }

        RaiseChanging(key, oldValue, value);
        RaiseChanged(key, oldValue, value);
    }

    private void RemoveString(string key)
    {
        if (!_items.Remove(key, out var oldValue))
        {
            return;
        }

        RaiseChanging(key, oldValue, null);
        RaiseChanged(key, oldValue, null);
    }

    private void RaiseChanging(string key, string? oldValue, string? newValue) =>
        Changing?.Invoke(this, new ChangingEventArgs
        {
            Key = key,
            OldValue = oldValue,
            NewValue = newValue
        });

    private void RaiseChanged(string key, string? oldValue, string? newValue) =>
        Changed?.Invoke(this, new ChangedEventArgs
        {
            Key = key,
            OldValue = oldValue,
            NewValue = newValue
        });
}
