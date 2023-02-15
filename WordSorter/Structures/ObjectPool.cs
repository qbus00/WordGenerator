namespace WordSorter.Structures;

public class ArraysPool<T>
{
    private readonly long _arraySize;

    public ArraysPool(long arraySize)
    {
        _arraySize = arraySize;
    }

    private readonly Queue<T[]> _pool = new();

    public T[] Get()
    {
        var array = _pool.Count < 1 ? new T[_arraySize] : _pool.Dequeue();
        return array;
    }

    public void Put(T[] array)
    {
        _pool.Enqueue(array);
    }
}
