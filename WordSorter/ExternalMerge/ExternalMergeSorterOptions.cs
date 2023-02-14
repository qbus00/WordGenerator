namespace WordSorter.ExternalMerge
{
    public class ExternalMergeSorterOptions
    {
        public ExternalMergeSorterOptions()
        {
            Split = new ExternalMergeSortSplitOptions();
            Sort = new ExternalMergeSortSortOptions();
            Merge = new ExternalMergeSortMergeOptions();
        }
        public ExternalMergeSortSplitOptions Split { get; init; }
        public ExternalMergeSortSortOptions Sort { get; init; }
        public ExternalMergeSortMergeOptions Merge { get; init; }
    }

    public class ExternalMergeSortSplitOptions
    {
        public long FileSize { get; init; } = 50 * 1024 * 1024;
        public char NewLineSeparator { get; init; } = '\n';
        public IProgress<double> ProgressHandler { get; init; } = null!;
    }

    public class ExternalMergeSortSortOptions
    {
        public IComparer<string> Comparer { get; init; } = Comparer<string>.Default;
        public int InputBufferSize { get; init; } = 65536;
        public int OutputBufferSize { get; init; } = 65536;
        public IProgress<double> ProgressHandler { get; init; } = null!;
        public int MaxNumberOfThreads { get; init; }
    }

    public class ExternalMergeSortMergeOptions
    {
        public int MaxNumberOfThreads { get; init; }
        public int InputBufferSize { get; init; } = 65536;
        public int OutputBufferSize { get; init; } = 65536;
        public IProgress<double> ProgressHandler { get; init; } = null!;
    }
}
