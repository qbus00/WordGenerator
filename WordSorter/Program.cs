
using System.Diagnostics;
using WordSorter;
using WordSorter.ExternalMerge;

var externalMergeSorter = new ExternalMergeSorter(new ExternalMergeSorterOptions
{
    Sort = new ExternalMergeSortSortOptions
    {
        Comparer = new CustomComparer()
    },
    Split = new ExternalMergeSortSplitOptions
    {
        FileSize = 512 * 1024 * 1024
    }
});

var timer = new Stopwatch();

Console.WriteLine("Sorting...");
timer.Start();
await using var fileStream = File.Open("data.txt", FileMode.Open);
await using var output = File.Create("output.txt");
await externalMergeSorter.Sort(fileStream, output, default);
timer.Stop();
Console.WriteLine($"Done. Sorting of file took {timer.Elapsed.TotalSeconds} seconds.");
