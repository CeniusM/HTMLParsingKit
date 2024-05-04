namespace HTMLParsingKit;

public class TagAttribute
{
    public string Name;
    public string Value;

    public TagAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public override string ToString()
    {
        if (Value == "")
            return Name;
        else
            return $"{Name}=\"{Value}\"";
    }
}

public class Element
{
    public string Name;
    public List<TagAttribute> Attributes;
    public ArraySegment<char> Content;

    // Contains the elements inside of Content
    public List<Element> Children;

    public bool HasChildren => Children.Count > 0;

    public Element(string tagName, List<TagAttribute> attributes, ArraySegment<char> content, List<Element> children)
    {
        Name = tagName;
        Attributes = attributes;
        Content = content;
        Children = children;
    }

    public bool ContainsAttribute(string att)
    {
        return Attributes.Any(x => x.Name == att);
    }

    public string GetAttribute(string att)
    {
        if (!ContainsAttribute(att))
            throw new Exception();

        return Attributes.Find(x => x.Name == att)!.Value;
    }

    public bool IsClass(string str)
    {
        return ContainsAttribute("class") && GetAttribute("class") == str;
    }

    public string ContentToString()
    {
        return new string(Content.ToArray());
    }

    public Element FirstChildOrSelf()
    {
        return IfAnyChildAtIndexOrSelf(0);
    }

    public Element IfAnyChildAtIndexOrSelf(int index)
    {
        return Children.Count == 0 ? this : Children[index];
    }

    public override string ToString()
    {
        return $"{Name} -> {Children.Count} : {(ContainsAttribute("class") ? GetAttribute("class") : "?")}";
    }
}

//class HTMLTree
//{
//}
