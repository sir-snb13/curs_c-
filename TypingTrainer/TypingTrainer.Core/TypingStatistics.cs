namespace TypingTrainer.Core;

public sealed class TypingStatistics
{
    public DateTime Date { get; set; }

    public string DictionaryName { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public double CharactersPerMinute { get; set; }

    public int ErrorsCount { get; set; }

    public double AccuracyPercent { get; set; }
}
