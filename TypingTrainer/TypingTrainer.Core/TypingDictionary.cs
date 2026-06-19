namespace TypingTrainer.Core;

public sealed class TypingDictionary
{
    public TypingDictionary()
    {
    }

    public TypingDictionary(string name, IEnumerable<string> phrases)
    {
        Name = name;
        Phrases = phrases.ToList();
    }

    public string Name { get; set; } = string.Empty;

    public List<string> Phrases { get; set; } = new();

    public override string ToString()
    {
        return Name;
    }
}
