using CommandLine;

namespace WordGenerator;

public class CommandLineOptions
{
    private int _maximumNumberOfWordsInSentence;

    [Option('w', "maximumNumberOfWordsInSentence",
        Required = false,
        Default = 5,
        HelpText = "Maximum number of words in one sentence. Default is 5. Maximum is 10.")]
    public int MaximumNumberOfWordsInSentence
    {
        get => _maximumNumberOfWordsInSentence;
        set
        {
            if (value is > 10 or < 1)
            {
                throw new InvalidDataException("Must be larger then 0 and smaller or equal to 10.");
            }

            _maximumNumberOfWordsInSentence = value;
        }
    }

    private int _maximumNumberOfSentences;

    [Option('m', "maximumNumberOfSentences",
        Required = false,
        Default = 1000,
        HelpText = "Maximum number of sentences to generate (0 = no limit). Default is 1000. Maximum is 1 000 000.")]
    public int MaximumNumberSentences
    {
        get => _maximumNumberOfSentences;
        set
        {
            if (value > 1_000_000)
            {
                throw new InvalidDataException("Must be smaller or equal to 1 000 000.");
            }

            _maximumNumberOfSentences = value;
        }
    }

    [Option('f', "filename",
        Required = true,
        HelpText = "Name of the file to generate data in.")]
    public string Filename { get; set; }

    private int _maximumRandomNumberToGenerate;

    [Option('n', "maximumRandomNumberToGenerate",
        Required = false,
        Default = 1000,
        HelpText = "Maximum number that can be generated to put at the beginning of a line. Default is 1000. Maximum is 1 000 000.")]
    public int MaximumRandomNumberToGenerate
    {
        get => _maximumRandomNumberToGenerate;
        set
        {
            if (value is > 1_000_000 or < 1)
            {
                throw new InvalidDataException("Must be larger then 1 and smaller or equal to 1 000 000.");
            }

            _maximumRandomNumberToGenerate = value;
        }
    }

    private int _sizeOfFileInGb;

    [Option('s', "sizeOfFileInGb",
        Required = true,
        HelpText = "Target size of the generated file in gigabytes. Minimum is 1. Maximum is 500.")]
    public int SizeOfFileInGb
    {
        get => _sizeOfFileInGb;
        set
        {
            if (value is > 500 or < 1)
            {
                throw new InvalidDataException("Must be larger then 1 and smaller or equal to 500.");
            }

            _sizeOfFileInGb = value;
        }
    }
}
