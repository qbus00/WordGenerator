namespace WordSorter;

public class CustomComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == null && y != null)
        {
            return -1;
        }

        if (y == null && x != null)
        {
            return 1;
        }

        if (x == null)
        {
            return 0;
        }

        var text1 = GetColumnValue(x, 2);
        var text2 = GetColumnValue(y, 2);

        var compareResult = text1.CompareTo(text2, StringComparison.OrdinalIgnoreCase);
        if (compareResult != 0)
        {
            return compareResult;
        }

        var success1 = IntParseFast(GetColumnValue(x, 1), out var number1);
        var success2 = IntParseFast(GetColumnValue(y, 1), out var number2);
        if (!success1 || !success2)
        {
            return compareResult;
        }

        if (number1 == number2)
        {
            return 0;
        }

        if (number1 > number2)
        {
            return 1;
        }

        return -1;
    }

    private static ReadOnlySpan<char> GetColumnValue(string value, int column)
    {
        var span = value.AsSpan();
        var columnCounter = 1;
        var columnStartIndex = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i].Equals('.'))
            {
                columnCounter++;
                continue;
            }

            if (columnCounter != column)
            {
                continue;
            }

            columnStartIndex = i;
            break;
        }

        var columnLength = 0;
        var slice = span[columnStartIndex..];
        foreach (var t in slice)
        {
            if (t != '.')
            {
                columnLength++;
            }
            else
            {
                break;
            }
        }

        return span.Slice(columnStartIndex, columnLength);
    }

    private static bool IntParseFast(ReadOnlySpan<char> s, out int result)
    {
        var value = 0;
        var length = s.Length;
        for (var i = 0; i < length; i++)
        {
            var c = s[i];
            if (!char.IsDigit(c))
            {
                result = -1;
                return false;
            }

            value = 10 * value + (c - 48);
        }

        result = value;
        return true;
    }
}
