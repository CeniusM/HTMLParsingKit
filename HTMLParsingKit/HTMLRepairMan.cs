namespace HTMLParsingKit;

// Helps to check for edge cases like using <p> and <p>, wich is not self enclosing, and the closing tag does not have /
public static class HTMLRepairMan
{
    // An attempt at fixing invalid html
    public static string Repair(string html)
    {
        List<char> list = [.. html];

        var placesToAddSlash = RepairNonSelfEnclosingTags(new ArraySegment<char>([.. list]));

        placesToAddSlash.Sort((x, y) => x.Item1 - y.Item1);

        int offset = 0;

        foreach (var (index, tag) in placesToAddSlash)
        {
            char[] insert = [.. "</", .. tag, .. ">"];

            list.InsertRange(index + offset, insert);

            int length = insert.Length;

            offset += length;
        }

        return new string(list.ToArray());
    }

    private static List<(int, char[])> RepairNonSelfEnclosingTags(ArraySegment<char> arr)
    {
        // This simply looks at what tags is not self enclosing
        // but does not have any closing tags like <p> content <p>

        // We just try and add a closing tag before the next tag or default clear tag (wich does not look at syntax...)

        // Ignores "

        List<(int, char[])> closingTagsToAdd = new List<(int, char[])>();

        var invalidTags = TagsUsed(arr)
            .Where(x => !x.Value)
            .Select(x => x.Key)
            .Where(x => !HTMLParser.SelfEnclosedTags.Contains(x))
            .ToArray();

        ArraySegment<char> defaultClearTags = new ArraySegment<char>([.. "<div class=clear>"]);

        foreach (var tag in invalidTags)
        {
            ArraySegment<char> tagSegment = new ArraySegment<char>([.. tag]);

            bool inComment = false;
            bool isInsideTag = false;

            for (int i = 0; i < arr.Length; i++)
            {
                ArraySegment<char> current = arr.Skip(i);

                if (inComment)
                {
                    if (current.Take(HTMLParser.CommentEnd.Length).IsMatch(HTMLParser.CommentEnd))
                    {
                        inComment = false;
                        continue;
                    }
                }
                else
                {
                    if (current.Take(HTMLParser.CommentStart.Length).IsMatch(HTMLParser.CommentStart))
                    {
                        inComment = true;
                        continue;
                    }
                }

                if (inComment)
                {
                    continue;
                }

                if (current[0] != '<')
                {
                    continue;
                }

                bool isTag = current.Skip(1).TakeWhile(HTMLParser.IsValidNameLetter).IsMatch(tagSegment);
                bool isClearTag = current.Take(defaultClearTags.Length).IsMatch(defaultClearTags);

                if (isTag || isClearTag)
                {
                    if (isInsideTag)
                    {
                        closingTagsToAdd.Add((i, tag.ToArray()));
                    }

                    isInsideTag = isTag; // else was clear
                }
            }
        }

        return closingTagsToAdd;
    }

    private static void Info() => Console.ForegroundColor = ConsoleColor.Cyan;
    private static void Good() => Console.ForegroundColor = ConsoleColor.Green;
    private static void Warning() => Console.ForegroundColor = ConsoleColor.Yellow;
    private static void Error() => Console.ForegroundColor = ConsoleColor.Red;

    // NOTE* should also look for an un even number of a certain tag. open to closing tag
    public static void DebugHTML(string html)
    {
        ArraySegment<char> arr = new ArraySegment<char>(html.ToArray());

        var tagsUsed = TagsUsed(arr);

        Info();
        Console.WriteLine("Tags used: ");
        foreach (var (key, slash) in tagsUsed)
        {
            if (slash)
                Console.Write("++");

            Console.WriteLine(key);
        }

        var weirdTags = tagsUsed
            .Where(x => !x.Value)
            .Select(x => x.Key)
            .Where(x => !HTMLParser.SelfEnclosedTags.Contains(x))
            .ToArray();

        if (weirdTags.Length == 0)
        {
            Good();
            Console.WriteLine("No weird tags");
        }
        else
        {
            Error();
            Console.WriteLine("Tags that is not self enclosing, yet have no end");

            foreach (var tag in weirdTags)
            {
                Console.WriteLine($"{{ {tag} }}");
            }
        }

        Console.ResetColor();
    }

    // DOES NOT IGNORE "" AND COMMENTS
    private static Dictionary<string, bool> TagsUsed(ArraySegment<char> arr)
    {
        Dictionary<string, bool> tags = new Dictionary<string, bool>();

        while (true)
        {
            arr = arr.SkipWhile(c => c != '<');

            if (arr.Length == 0)
                break;

            arr = arr.Skip(1);

            if (arr[0] == '!')
                continue;

            bool hasSlash = arr[0] == '/';

            if (hasSlash)
                arr = arr.Skip(1);

            var tag = new string(arr.TakeWhile(HTMLParser.IsValidNameLetter).ToArray());

            if (tags.ContainsKey(tag))
            {
                if (hasSlash)
                    tags[tag] = true;

                continue;
            }

            tags.Add(tag, hasSlash);
        }

        return tags;
    }
}
