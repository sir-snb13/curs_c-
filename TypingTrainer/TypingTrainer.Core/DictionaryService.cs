using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypingTrainer.Core;

public sealed class DictionaryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public DictionaryService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "dictionaries.json");
    }

    public IReadOnlyList<TypingDictionary> LoadDictionaries()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = CreateDefaultDictionaries();
            SaveDictionaries(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var dictionaries = JsonSerializer.Deserialize<List<TypingDictionary>>(json, JsonOptions);

            if (dictionaries is { Count: > 0 })
            {
                Normalize(dictionaries);
                if (MergeDefaultDictionaries(dictionaries))
                {
                    SaveDictionaries(dictionaries);
                }

                return dictionaries;
            }
        }
        catch (JsonException)
        {
        }

        return CreateDefaultDictionaries();
    }

    public void SaveDictionaries(IEnumerable<TypingDictionary> dictionaries)
    {
        var normalized = dictionaries
            .Where(dictionary => !string.IsNullOrWhiteSpace(dictionary.Name))
            .Select(dictionary => new TypingDictionary(
                dictionary.Name.Trim(),
                dictionary.Phrases.Where(phrase => !string.IsNullOrWhiteSpace(phrase)).Select(phrase => phrase.Trim())))
            .ToList();

        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public bool AddDictionary(List<TypingDictionary> dictionaries, string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) ||
            dictionaries.Any(dictionary => string.Equals(dictionary.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        dictionaries.Add(new TypingDictionary(name, Array.Empty<string>()));
        SaveDictionaries(dictionaries);
        return true;
    }

    public bool RemoveDictionary(List<TypingDictionary> dictionaries, string name)
    {
        var removed = dictionaries.RemoveAll(dictionary =>
            string.Equals(dictionary.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            SaveDictionaries(dictionaries);
        }

        return removed;
    }

    public bool AddPhrase(List<TypingDictionary> dictionaries, string dictionaryName, string phrase)
    {
        phrase = phrase.Trim();
        var dictionary = dictionaries.FirstOrDefault(item =>
            string.Equals(item.Name, dictionaryName, StringComparison.OrdinalIgnoreCase));

        if (dictionary is null || string.IsNullOrWhiteSpace(phrase) || dictionary.Phrases.Contains(phrase))
        {
            return false;
        }

        dictionary.Phrases.Add(phrase);
        SaveDictionaries(dictionaries);
        return true;
    }

    public bool RemovePhrase(List<TypingDictionary> dictionaries, string dictionaryName, string phrase)
    {
        var dictionary = dictionaries.FirstOrDefault(item =>
            string.Equals(item.Name, dictionaryName, StringComparison.OrdinalIgnoreCase));

        if (dictionary is null)
        {
            return false;
        }

        var removed = dictionary.Phrases.Remove(phrase);
        if (removed)
        {
            SaveDictionaries(dictionaries);
        }

        return removed;
    }

    private static List<TypingDictionary> CreateDefaultDictionaries()
    {
        return new List<TypingDictionary>
        {
            new("Простые слова", new[]
            {
                "мама",
                "папа",
                "дом",
                "кот",
                "лес",
                "мир",
                "мост",
                "река",
                "город",
                "школа",
                "окно",
                "книга",
                "ручка",
                "парта",
                "солнце",
                "ветер",
                "дождь",
                "улица",
                "работа",
                "друзья",
                "дорога",
                "машина",
                "комната",
                "клавиша"
            }),
            new("Простые фразы", new[]
            {
                "как дела",
                "сегодня хороший день",
                "я учусь быстро печатать",
                "это простой текст для тренировки",
                "утром я пью горячий чай",
                "мы читаем интересную книгу",
                "на столе лежит синяя ручка",
                "около дома растёт высокий клён",
                "я спокойно набираю каждую букву",
                "быстрая печать приходит с практикой",
                "важно смотреть на текст а не на клавиатуру",
                "каждая новая попытка становится лучше",
                "ошибки помогают заметить слабые места",
                "сегодня я напечатаю больше чем вчера"
            }),
            new("Скороговорки", new[]
            {
                "шла саша по шоссе",
                "карл у клары украл кораллы",
                "на дворе трава на траве дрова",
                "тридцать три корабля лавировали",
                "от топота копыт пыль по полю летит",
                "у ежа ежата у ужа ужата",
                "кукушка кукушонку купила капюшон",
                "везёт сенька саньку с сонькой на санках"
            }),
            new("Тексты для тренировки", new[]
            {
                "печать без спешки помогает держать ровный ритм и меньше ошибаться",
                "сначала важна точность а скорость постепенно вырастет сама",
                "короткие тренировки каждый день дают лучший результат чем редкие длинные занятия",
                "когда пальцы привыкают к клавишам текст набирается спокойнее и увереннее",
                "хорошая тренировка начинается с удобной посадки и ровного дыхания",
                "не нужно исправлять каждую ошибку во время упражнения просто продолжайте печатать",
                "чем больше разных слов встречается в тексте тем полезнее становится тренировка",
                "постепенно можно переходить от простых слов к длинным предложениям",
                "внимательно следите за следующей буквой и сохраняйте одинаковый темп",
                "после нескольких попыток сравните скорость точность и количество ошибок"
            })
        };
    }

    private static bool MergeDefaultDictionaries(List<TypingDictionary> dictionaries)
    {
        var changed = false;

        foreach (var defaultDictionary in CreateDefaultDictionaries())
        {
            var dictionary = dictionaries.FirstOrDefault(item =>
                string.Equals(item.Name, defaultDictionary.Name, StringComparison.OrdinalIgnoreCase));

            if (dictionary is null)
            {
                dictionaries.Add(defaultDictionary);
                changed = true;
                continue;
            }

            foreach (var phrase in defaultDictionary.Phrases)
            {
                if (!dictionary.Phrases.Contains(phrase))
                {
                    dictionary.Phrases.Add(phrase);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static void Normalize(List<TypingDictionary> dictionaries)
    {
        foreach (var dictionary in dictionaries)
        {
            dictionary.Name = dictionary.Name.Trim();
            dictionary.Phrases = dictionary.Phrases
                .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
                .Select(phrase => phrase.Trim())
                .Distinct()
                .ToList();
        }
    }
}
