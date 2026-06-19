namespace TypingTrainer.Core;

public enum TrainingDurationMode
{
    Unlimited,
    OneMinute,
    ThreeMinutes,
    FiveMinutes
}

public enum AppTheme
{
    Light,
    Dark
}

public sealed class AppSettings
{
    public int FontSize { get; set; } = 18;

    public bool ShowKeyboard { get; set; } = true;

    public TrainingDurationMode TrainingDurationMode { get; set; } = TrainingDurationMode.Unlimited;

    public AppTheme Theme { get; set; } = AppTheme.Light;

    public int? GetDurationLimitSeconds()
    {
        return TrainingDurationMode switch
        {
            TrainingDurationMode.OneMinute => 60,
            TrainingDurationMode.ThreeMinutes => 180,
            TrainingDurationMode.FiveMinutes => 300,
            _ => null
        };
    }
}
