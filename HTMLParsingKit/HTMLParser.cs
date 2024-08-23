namespace HTMLParsingKit;

public static class HTMLParser
{
    public static readonly char[] ValidNameLetters = ['!', '_', '-'];

    public static readonly string[] SelfEnclosedTags =
        ["!DOCTYPE", "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"];

    public static readonly string[] UnsupportedTags = [];

    internal static readonly ArraySegment<char> CommentStart = new ArraySegment<char>("<!--".ToArray());
    internal static readonly ArraySegment<char> CommentEnd = new ArraySegment<char>("-->".ToArray());

    internal static readonly ArraySegment<char> ScriptStart = new ArraySegment<char>("<script".ToArray());
    internal static readonly ArraySegment<char> ScriptEnd = new ArraySegment<char>("</script>".ToArray());

    public static bool IsValidNameLetter(char c)
    {
        return char.IsLetterOrDigit(c) || ValidNameLetters.Contains(c);
    }

    public static List<Element> GenerateTree(string str)
    {

        // ADD ROOT AND MAKE THE ELEMENTS HAVE THEIR PARREN PLZ "
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
            arr = SkipToNext(arr, '<', ref skipped);

            // Leave if nothing left
            if (arr.Length == 0)
            {
                return (skipped, elements);
            }

            // Skip comments
            if (arr.Take(CommentStart.Length).IsMatch(CommentStart))
            {
                arr = Skip(arr, CommentStart.Length, ref skipped, out _);

                arr = SkipTillMatch(arr, CommentEnd, ref skipped, out _);

                arr = Skip(arr, CommentEnd.Length, ref skipped, out _);

                // Skipped comment and try again
                continue;
            }

            // For now we also skip script
            if (arr.Take(ScriptStart.Length).IsMatch(ScriptStart))
            {
                int scriptTagLength = TagLength(arr, 0);

                arr = Skip(arr, scriptTagLength, ref skipped, out var openingScriptTag);

                arr = SkipTillMatch(arr, ScriptEnd, ref skipped, out var scriptContent);

                arr = Skip(arr, ScriptEnd.Length, ref skipped, out var closingScriptTag);

                elements.Add(
                    new Element(
                        "script",
                        ParseOpeningTagAttributes(openingScriptTag),
                        scriptContent,
                        new List<Element>()));

                continue;
            }

            // Leave if hit closing tag
            if (arr[1] == '/')
            {
                return (skipped, elements);
            }

            // Now parse the current element
            string tagName = SegmentAsString(arr.Skip(1).TakeWhile(IsValidNameLetter));

            if (UnsupportedTags.Contains(tagName))
                throw new Exception($"Unsupported tag: {tagName}");

            int tagLength = TagLength(arr, 0);

            arr = Skip(arr, tagLength, ref skipped, out var openingTag);

            // If it is selfinclosing we skip the search
            if (SelfEnclosedTags.Contains(tagName))
            {
                elements.Add(
                    new Element(
                        tagName,
                        tagName == "!DOCTYPE" ? new List<TagAttribute>() : ParseOpeningTagAttributes(openingTag),
                        openingTag,
                        new List<Element>()));

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
                    content,
                    search.elements));
        }
    }

    private static List<TagAttribute> ParseOpeningTagAttributes(ArraySegment<char> arr)
    {
        string div = SegmentAsString(arr);

        var result = new List<TagAttribute>();

        arr = arr
            .Skip(TextSearch.Length(arr, 1, IsValidNameLetter) + 1)
            .SkipLast(1);

        while (arr.Length > 0)
        {
            arr = arr.SkipWhile(char.IsWhiteSpace);

            if (arr.Length == 0)
                break;

            if (arr.Length == 1 && arr[0] == '/')
                break;

            int attNameSize = TextSearch.Length(arr, 0, IsValidNameLetter);

            var attNameSkipped = arr.Skip(attNameSize);
            bool hasValue = attNameSkipped.Length != 0 && attNameSkipped[0] == '=';

            if (!hasValue)
            {
                result.Add(new TagAttribute(
                    SegmentAsString(arr.Take(attNameSize)),
                    ""
                    ));

                arr = attNameSkipped;

                continue;
            }

            var skipSegment = attNameSkipped
                .SkipWhile(char.IsWhiteSpace)
                .Skip(1)
                .SkipWhile(char.IsWhiteSpace);

            // value=
            // or
            // value="
            bool isStr = skipSegment[0] == '"';

            int skipSize = arr.Length - skipSegment.Length - attNameSize + (isStr ? 1 : 0);

            int valueSize = arr
                .Skip(attNameSize + skipSize)
                .TakeWhile(c => isStr ? c != '"' : c != ' ')
                .Length;

            result.Add(new TagAttribute(
                SegmentAsString(arr.Take(attNameSize)),
                SegmentAsString(arr.Skip(attNameSize + skipSize).Take(valueSize))
                ));

            int stride = attNameSize + valueSize + skipSize + (isStr ? 1 : 0);

            arr = arr.Skip(stride);
        }

        return result;
    }

    private static ArraySegment<char> SkipToNext(ArraySegment<char> arr, char end, ref int skipped)
    {
        int length = arr.TakeWhile(c => c != end).Length;

        skipped += length;

        return arr.Skip(length);
    }

    private static ArraySegment<char> Skip(ArraySegment<char> arr, int length, ref int skipped, out ArraySegment<char> cut)
    {
        cut = arr.Take(length);

        skipped += length;

        return arr.Skip(length);
    }

    private static ArraySegment<char> SkipTillMatch(ArraySegment<char> arr, ArraySegment<char> match, ref int skipped, out ArraySegment<char> cut)
    {
        int length = 0;

        while (!arr.Skip(length).Take(match.Length).IsMatch(match))
            length++;

        cut = arr.Take(length);

        return Skip(arr, length, ref skipped, out _);
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
