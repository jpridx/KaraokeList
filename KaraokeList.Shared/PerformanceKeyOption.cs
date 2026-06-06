namespace KaraokeList.Shared;

public sealed class PerformanceKeyOption
{
    public int? Semitones { get; init; }
    public string Label { get; init; } = string.Empty;

    public static IReadOnlyList<PerformanceKeyOption> Choices { get; } =
    [
        new() { Semitones = null, Label = "Original key" },
        new() { Semitones = -3, Label = "Down 3" },
        new() { Semitones = -2, Label = "Down 2" },
        new() { Semitones = -1, Label = "Down 1" },
        new() { Semitones = 0, Label = "Original (0)" },
        new() { Semitones = 1, Label = "Up 1" },
        new() { Semitones = 2, Label = "Up 2" },
        new() { Semitones = 3, Label = "Up 3" },
    ];
}
