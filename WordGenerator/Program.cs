using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using CommandLine;
using WordGenerator;

const int numberOfBytesInGigabyte = 1_073_741_824;

var parseResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
if (parseResult.Errors.Any())
{
    return 0;
}

await Console.Out.WriteLineAsync("Generating...");
var timer = new Stopwatch();

timer.Start();

var englishWords = await GetEnglishWordsFromFile();
var sizeOfFileInBytes = (long)parseResult.Value.SizeOfFileInGb * numberOfBytesInGigabyte;
var random = new Random();
var textInfo = CultureInfo.InvariantCulture.TextInfo;
var generatedSentencesMaximumSize = parseResult.Value.MaximumNumberSentences;
var maximumRandomNumberToGenerate = parseResult.Value.MaximumRandomNumberToGenerate;
var sentenceSize = parseResult.Value.MaximumNumberOfWordsInSentence;
var generatedSentences = new List<string>(generatedSentencesMaximumSize);

await using (var fileStream = File.CreateText(parseResult.Value.Filename))
{
    while (fileStream.BaseStream.Length < sizeOfFileInBytes)
    {
        var number = random.Next(0, maximumRandomNumberToGenerate + 1);
        var sentence = generatedSentences.Count == generatedSentencesMaximumSize && generatedSentencesMaximumSize > 0
            ? generatedSentences[random.Next(0, generatedSentencesMaximumSize)]
            : GenerateSentence();

        var line = number.ToString(CultureInfo.InvariantCulture) + ". " + sentence;
        await fileStream.WriteLineAsync(line);
    }

    await fileStream.FlushAsync();
    fileStream.Close();
}

timer.Stop();
Console.WriteLine($"Done. Generating of file took {timer.Elapsed.TotalSeconds} seconds.");

return 0;

async Task<string[]> GetEnglishWordsFromFile()
{
    var assembly = Assembly.GetExecutingAssembly();
    var englishWordsFileResourceName = "WordGenerator.Resources.words_alpha.txt";
    await using var stream = assembly.GetManifestResourceStream(englishWordsFileResourceName);
    using var reader = new StreamReader(stream);
    var strings = (await reader.ReadToEndAsync()).Split(new[] { "\r\n" }, StringSplitOptions.None);
    return strings;
}

string GenerateSentence()
{
    var numberOfWords = random.Next(1, sentenceSize + 1);
    var sentenceBuilder = new StringBuilder();
    for (var i = 1; i <= numberOfWords; i++)
    {
        sentenceBuilder.Append(englishWords[random.Next(0, englishWords.Length)]).Append(" ");
    }

    var sentence = textInfo.ToTitleCase(sentenceBuilder.ToString().Trim());
    if (generatedSentencesMaximumSize > 0)
    {
        generatedSentences.Add(sentence);
    }

    return sentence;
}
