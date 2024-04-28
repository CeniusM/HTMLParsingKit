namespace HTMLParsingKit;

class TextSearch
{
    public static int WordLength(ArraySegment<char> buffer, int start)
    {
        return Length(buffer, start, char.IsLetter);
    }

    public static int Length(ArraySegment<char> buffer, int start, Func<char, bool> IsCharValid)
    {
        int index = start;

        while (IsCharValid(buffer[index]) && index <= buffer.Length)
        {
            index++;
        }

        return index - start;
    }

    public static ArraySegment<char> Word(ArraySegment<char> buffer, int start)
    {
        int wordLength = WordLength(buffer, start);

        return
            buffer
            .Skip(start)
            .Take(wordLength);
    }
}
