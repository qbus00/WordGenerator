using CommandLine;

namespace WordSorter;

public class CommandLineOptions
{
    private int _numberOfThreads;

    [Option('n', "numberOfThreads",
        Required = false,
        Default = 2,
        HelpText = "Maximum number of simultaneous threads while sorting. Maximum is 256.")]
    public int NumberOfThreads
    {
        get => _numberOfThreads;
        set
        {
            if (value is > 256 or < 1)
            {
                throw new InvalidDataException("Must be larger then 0 and smaller or equal to 256.");
            }

            _numberOfThreads = value;
        }
    }

    [Option('i', "inputFilename",
        Required = true,
        HelpText = "Name of the file to be sorted.")]
    public string InputFilename { get; set; }

    [Option('o', "outputFilename",
        Required = true,
        HelpText = "Name of the output file.")]
    public string OutputFilename { get; set; }

    private int _sizeOfTemporaryFiles;

    [Option('s', "sizeOfTemporaryFiles",
        Required = false,
        Default = 512,
        HelpText = "Size of temporary files used in K-Way merge sorting in MB. Maximum is 1536MB.")]
    public int SizeOfTemporaryFiles
    {
        get => _sizeOfTemporaryFiles;
        set
        {
            if (value is > 1536 or < 1)
            {
                throw new InvalidDataException("Must be larger then 0 and smaller or equal to 1048576.");
            }

            _sizeOfTemporaryFiles = value;
        }
    }
}
