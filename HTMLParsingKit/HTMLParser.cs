namespace HTMLParsingKit;

class HTMLParser
{
    public static readonly string[] SelfEnclosedTags = ["!DOCTYPE", "br", "img", "link", "meta", "input", "hr", "area"];

    public static readonly string[] UnsupportedTags = ["script"];

    public static List<Element> GenerateTree(string str)
    {
        return ParseElements(new ArraySegment<char>(str.ToCharArray())).elements;
    }

    // Should return total size and just the content without the tags so we can get just the text
    private static (int size, List<Element> elements) ParseElements(ArraySegment<char> arr)
    {
        if (arr.Length == 0)
            throw new Exception();

        List<Element> elements = new List<Element>();

        int skipped = 0;

        // Loop over tags
        while (true)
        {
            // Move to next tag
            arr = GoToNext(arr, '<', ref skipped);

            // Leave if nothing left
            if (arr.Length == 0)
            {
                return (skipped, elements);
            }

            // This should also be done with script....
            if (arr[0] == '<' && arr[1] == '!' && arr[2] == '-' && arr[3] == '-')
            {
                // <!--comment-->

                int commentSize = 3;

                while (
                    arr[commentSize + 0] != '-' ||
                    arr[commentSize + 1] != '-' ||
                    arr[commentSize + 2] != '>'
                    )
                    commentSize++;

                commentSize += 3;

                arr = Skip(arr, commentSize, ref skipped, out var comment);

                // Skipped comment and try again
                continue;
            }

            // Leave if hit closing tag
            if (arr[1] == '/')
            {
                return (skipped, elements);
            }

            // Now parse the current element
            string tagName = SegmentAsString(TextSearch.Word(arr, 1));

            if (UnsupportedTags.Contains(tagName))
                throw new Exception($"Unsupported tag: {tagName}");

            int tagLength = TagLength(arr, 0);

            arr = Skip(arr, tagLength, ref skipped, out var openingTag);

            // Skip if tag is self enclosing
            if (SelfEnclosedTags.Contains(tagName))
            {
                continue;
            }

            // Parse content
            var search = ParseElements(arr);

            // Skip content
            arr = Skip(arr, search.size, ref skipped, out var content);

            // Skip closing tag
            int endTagLength = TagLength(arr, 0);

            arr = Skip(arr, endTagLength, ref skipped, out var clossingTag);

            elements.Add(
                new Element(
                    tagName,
                    ParseOpeningTagAttributes(openingTag),
                    arr,
                    search.elements));
        }
    }

    private static List<TagAttribute> ParseOpeningTagAttributes(ArraySegment<char> arr)
    {
        var result = new List<TagAttribute>();

        arr = arr
            .Skip(TextSearch.Length(arr, 1, char.IsLetterOrDigit) + 2)
            .SkipLast(1);

        while (arr.Length > 0)
        {
            int attNameSize = TextSearch.Length(arr, 0, (c) => char.IsLetterOrDigit(c) || c == '-');

            int valueSize = arr
                .Skip(attNameSize + 2)
                .TakeWhile(c => c != '"')
                .Length;

            result.Add(new TagAttribute(
                SegmentAsString(arr.Take(attNameSize)),
                SegmentAsString(arr.Skip(attNameSize + 2).Take(valueSize))
                ));

            int stride = attNameSize + valueSize + 3;
            arr = arr.Skip(stride).SkipWhile(char.IsWhiteSpace);
        }

        return result;
    }

    private static ArraySegment<char> GoToNext(ArraySegment<char> arr, char end, ref int skipped)
    {
        int length = arr.SkipWhile(c => c != end).Length;

        skipped += length;

        return arr.Skip(length);
    }

    private static ArraySegment<char> Skip(ArraySegment<char> arr, int length, ref int skipped, out ArraySegment<char> cut)
    {
        cut = arr.Take(length);

        skipped += length;

        return arr.Skip(length);
    }

    // Finds the length to next >, that is not inside " "
    private static int TagLength(ArraySegment<char> arr, int start)
    {
        bool inString = false;

        int size = 0;

        while (true)
        {
            char c = arr[start + size++];

            if (c == '"')
            {
                inString = !inString;
            }

            if (!inString && c == '>')
            {
                return size;
            }
        }
    }

    private static string SegmentAsString(ArraySegment<char> arr)
    {
        return new string(arr.ToArray());
    }
}
