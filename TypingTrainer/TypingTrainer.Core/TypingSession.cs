namespace TypingTrainer.Core;

public sealed class TypingSession
{
    public TypingSession()
    {
    }

    public TypingSession(string sourceText)
    {
        SourceText = sourceText;
    }

    public string SourceText { get; set; } = string.Empty;

    public string UserInput { get; private set; } = string.Empty;

    public DateTime StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public int ErrorsCount { get; private set; }

    public bool IsCompleted { get; private set; }

    public void Start()
    {
        UserInput = string.Empty;
        ErrorsCount = 0;
        IsCompleted = false;
        StartTime = DateTime.Now;
        EndTime = null;
    }

    public void Reset()
    {
        UserInput = string.Empty;
        ErrorsCount = 0;
        IsCompleted = false;
        StartTime = default;
        EndTime = null;
    }

    public void UpdateInput(string userInput)
    {
        UserInput = userInput;
        ErrorsCount = CountErrors(userInput);

        if (!IsCompleted && SourceText.Length > 0 && userInput.Length >= SourceText.Length)
        {
            Finish();
        }
    }

    public void Finish()
    {
        IsCompleted = true;
        EndTime ??= DateTime.Now;
    }

    public double CalculateSpeed()
    {
        var duration = GetDuration();
        if (duration.TotalSeconds <= 0)
        {
            return 0;
        }

        return UserInput.Length / duration.TotalMinutes;
    }

    public double CalculateAccuracy()
    {
        if (UserInput.Length == 0)
        {
            return 100;
        }

        var correctCharacters = Math.Max(0, UserInput.Length - ErrorsCount);
        return correctCharacters * 100.0 / UserInput.Length;
    }

    public TimeSpan GetDuration()
    {
        if (StartTime == default)
        {
            return TimeSpan.Zero;
        }

        return (EndTime ?? DateTime.Now) - StartTime;
    }

    public int? GetFirstErrorPosition()
    {
        for (var index = 0; index < UserInput.Length; index++)
        {
            if (index >= SourceText.Length || !IsCharacterCorrectAt(index))
            {
                return index + 1;
            }
        }

        return null;
    }

    public bool IsCharacterCorrectAt(int index)
    {
        if (index < 0 || index >= UserInput.Length || index >= SourceText.Length)
        {
            return false;
        }

        return AreSamePositionCharactersEqual(SourceText[index], UserInput[index]);
    }

    private int CountErrors(string userInput)
    {
        var errors = 0;
        var comparedLength = Math.Min(userInput.Length, SourceText.Length);

        for (var index = 0; index < comparedLength; index++)
        {
            if (!AreSamePositionCharactersEqual(SourceText[index], userInput[index]))
            {
                errors++;
            }
        }

        return errors + Math.Max(0, userInput.Length - SourceText.Length);
    }

    private static bool AreSamePositionCharactersEqual(char sourceCharacter, char inputCharacter)
    {
        return char.ToUpperInvariant(sourceCharacter) == char.ToUpperInvariant(inputCharacter);
    }
}
