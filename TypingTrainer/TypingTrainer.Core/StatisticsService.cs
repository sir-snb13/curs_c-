using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypingTrainer.Core;

public sealed class StatisticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public StatisticsService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "statistics.json");
    }

    public IReadOnlyList<TypingStatistics> LoadStatistics()
    {
        if (!File.Exists(_filePath))
        {
            SaveStatistics(Array.Empty<TypingStatistics>());
            return Array.Empty<TypingStatistics>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<TypingStatistics>>(json, JsonOptions) ?? new List<TypingStatistics>();
        }
        catch (JsonException)
        {
            return Array.Empty<TypingStatistics>();
        }
    }

    public void SaveStatistics(IEnumerable<TypingStatistics> statistics)
    {
        var json = JsonSerializer.Serialize(statistics, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void AddResult(TypingStatistics result)
    {
        var statistics = LoadStatistics().ToList();
        statistics.Add(result);
        SaveStatistics(statistics);
    }

    public double GetBestSpeed(IEnumerable<TypingStatistics>? statistics = null)
    {
        var source = statistics ?? LoadStatistics();
        return source.Any() ? source.Max(item => item.CharactersPerMinute) : 0;
    }

    public double GetAverageAccuracy(IEnumerable<TypingStatistics>? statistics = null)
    {
        var source = statistics ?? LoadStatistics();
        return source.Any() ? source.Average(item => item.AccuracyPercent) : 0;
    }

    public int GetTotalErrors(IEnumerable<TypingStatistics>? statistics = null)
    {
        var source = statistics ?? LoadStatistics();
        return source.Sum(item => item.ErrorsCount);
    }
}
