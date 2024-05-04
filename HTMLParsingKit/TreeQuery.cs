namespace HTMLParsingKit;

public static class TreeQuery
{
    public static List<Element> Filter(List<Element> elements, Func<Element, bool> filter)
    {
        return elements.Where(filter).ToList();
    }

    public static List<Element> GetChildren(List<Element> elements)
    {
        return elements.SelectMany(e => e.Children).ToList();
    }
}
