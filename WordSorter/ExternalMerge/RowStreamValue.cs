namespace WordSorter.ExternalMerge;

internal readonly struct RowStreamValue
{
    public string Value { get; init; }
    public int StreamReader { get; init; }
}
