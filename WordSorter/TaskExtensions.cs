using System.Collections.Concurrent;

namespace WordSorter;

public static class TaskExtensions
{
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
    {
        return Task.WhenAll(
            Partitioner.Create(source).GetPartitions(dop).Select(partition =>
                Task.Run(async () =>
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                await body(partition.Current);
                            }
                        }
                    }
                )));
    }
}
