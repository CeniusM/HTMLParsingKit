namespace HTMLParsingKit;

public class ArraySegment<T>
{
    private T[] _arr;
    private int _start;
    private int _end;

    public int Length => _end - _start;

    public ArraySegment(T[] arr)
    {
        _arr = arr;
        _start = 0;
        _end = arr.Length;
    }

    private ArraySegment(ArraySegment<T> parrent, int offset, int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException($"{nameof(length)} can not be below 0");

        _arr = parrent._arr;
        _start = parrent._start + offset;
        _end = _start + length;

        // Can not make a new segment outside of the parrents segment
        _start = Math.Clamp(_start, parrent._start, parrent._end);
        _end = Math.Clamp(_end, parrent._start, parrent._end);
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

    // Checks if the segment is equal
    public bool IsMatch(ArraySegment<T> segment)
    {
        if (Length != segment.Length)
            return false;

        for (int i = 0; i < Length; i++)
        {
            T first = this[i];
            T second = segment[i];

            if (first is null && second is null)
                continue;

            if (first is null)
                return false;

            if (!first.Equals(second))
                return false;
        }

        return true;
    }

    public T[] ToArray()
    {
        T[] result = new T[Length];
        Array.Copy(_arr, _start, result, 0, Length);
        return result;
    }

    public T this[int param]
    {
        get
        {
            int index = param + _start;

            if (index >= _end)
                throw new IndexOutOfRangeException();
            if (index < _start)
                throw new IndexOutOfRangeException();

            return _arr[index];
        }
    }

    public override string ToString()
    {
        return $"({Length}): {string.Join(", ", ToArray())}";
    }
}