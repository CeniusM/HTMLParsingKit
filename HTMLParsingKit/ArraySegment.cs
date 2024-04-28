namespace HTMLParsingKit;

class ArraySegment<T>
{
    private T[] _arr;
    private int start;
    private int end;

    public int Length => end - start;

    public ArraySegment(T[] arr)
    {
        _arr = arr;
        start = 0;
        end = arr.Length;
    }

    private ArraySegment(ArraySegment<T> arr, int offset, int length)
    {
        _arr = arr._arr;
        start = arr.start + offset;
        end = start + length;

        start = Math.Clamp(start, 0, _arr.Length);
        end = Math.Clamp(end, 0, _arr.Length);
    }

    public ArraySegment<T> Take(int count)
    {
        return new ArraySegment<T>(this, 0, count);
    }

    public ArraySegment<T> Skip(int count)
    {
        return new ArraySegment<T>(this, count, Length - count);
    }

    public ArraySegment<T> SkipLast(int count)
    {
        return new ArraySegment<T>(this, 0, Length - count);
    }

    public ArraySegment<T> SkipWhile(Func<T, bool> func)
    {
        int offset = 0;

        while (offset < Length && func(this[offset]))
        {
            offset++;
        }

        return Skip(offset);
    }

    public ArraySegment<T> TakeWhile(Func<T, bool> func)
    {
        int offset = 0;

        while (offset < Length && func(this[offset]))
        {
            offset++;
        }

        return Take(offset);
    }

    public T[] ToArray()
    {
        T[] result = new T[Length];
        Array.Copy(_arr, start, result, 0, Length);
        return result;
    }

    public T this[int param]
    {
        get
        {
            int index = param + start;

            if (index >= end)
                throw new IndexOutOfRangeException();
            if (index < start)
                throw new IndexOutOfRangeException();

            return _arr[index];
        }
    }
}