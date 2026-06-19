namespace TypingTrainer.Desktop;

public sealed class CommandLineOptions
{
    public bool ShowHelp { get; private init; }

    public string? DictionaryName { get; private init; }

    public static CommandLineOptions Parse(string[] args)
    {
        var showHelp = false;
        string? dictionaryName = null;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;
                continue;
            }

            if (string.Equals(arg, "--dictionary", StringComparison.OrdinalIgnoreCase) &&
                index + 1 < args.Length)
            {
                dictionaryName = args[index + 1];
                index++;
            }
        }

        return new CommandLineOptions
        {
            ShowHelp = showHelp,
            DictionaryName = dictionaryName
        };
    }
}
