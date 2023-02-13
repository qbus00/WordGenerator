using CommandLine;
using ShellProgressBar;
using WordSorter;
using WordSorter.ExternalMerge;

var parseResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
if (parseResult.Errors.Any())
{
    return 0;
}

var disablePercentageOption = new ProgressBarOptions { DisableBottomPercentage = true };
var enablePercentageOption = new ProgressBarOptions { DisableBottomPercentage = false };

using var progressBar = new ProgressBar(10_000, "Sorting data using K-Way merge external sort.", disablePercentageOption);
var merge = progressBar.Spawn(10_000, "3. Merging temporary files using K-Way merge", enablePercentageOption);
var sort = progressBar.Spawn(10_000, "2. Sorting data in files", enablePercentageOption);
var split = progressBar.Spawn(10_000, "1. Splitting files", enablePercentageOption);
var externalMergeSorter = new ExternalMergeSorter(new ExternalMergeSorterOptions
{
    Split = new ExternalMergeSortSplitOptions
    {
        ProgressHandler = split.AsProgress<double>(),
        FileSize = parseResult.Value.SizeOfTemporaryFiles * 1024L * 1024L
    },
    Sort = new ExternalMergeSortSortOptions
    {
        ProgressHandler = sort.AsProgress<double>(),
        MaxNumberOfThreads = parseResult.Value.NumberOfThreads,
        Comparer = new CustomComparer()
    },
    Merge = new ExternalMergeSortMergeOptions
    {
        ChunkFilesStep = parseResult.Value.ChunkFilesStep,
        MaxNumberOfThreads = parseResult.Value.NumberOfThreads,
        ProgressHandler = merge.AsProgress<double>(),
    }
});
await externalMergeSorter.Sort(parseResult.Value.InputFilename, parseResult.Value.OutputFilename, default);
return 0;
