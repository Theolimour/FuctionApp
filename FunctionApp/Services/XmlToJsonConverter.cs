using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace FunctionApp.Services;

/// <summary>
/// Converts an XML payload into a JSON array.
/// Each repeating child of the root (or each top-level fragment element) becomes one array item.
/// </summary>
public sealed class XmlToJsonConverter : IXmlToJsonConverter
{
    public JsonArray Convert(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new XmlPayloadException("Request body is empty.");
        }

        var (root, wrappedAsFragment) = Parse(xml.Trim());
        var items = SelectArrayItems(root, wrappedAsFragment);

        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(ElementToNode(item));
        }

        return array;
    }

    private static (XElement Root, bool WrappedAsFragment) Parse(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            if (document.Root is null)
            {
                throw new XmlPayloadException("XML document has no root element.");
            }

            return (document.Root, false);
        }
        catch (XmlException)
        {
            try
            {
                var wrapped = XDocument.Parse($"<payload>{xml}</payload>");
                return (wrapped.Root!, true);
            }
            catch (XmlException ex)
            {
                throw new XmlPayloadException("The request body is not valid XML.", ex);
            }
        }
    }

    private static IEnumerable<XElement> SelectArrayItems(XElement root, bool wrappedAsFragment)
    {
        var children = root.Elements().ToList();

        if (wrappedAsFragment)
        {
            return children;
        }

        if (children.Count == 0)
        {
            return [root];
        }

        var distinctNames = children.Select(e => e.Name.LocalName).Distinct().Count();
        if (distinctNames == 1)
        {
            return children;
        }

        return [root];
    }

    private static JsonNode? ElementToNode(XElement element)
    {
        var attributes = element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToList();
        var children = element.Elements().ToList();
        var text = GetDirectText(element);

        if (children.Count == 0 && attributes.Count == 0)
        {
            return JsonValue.Create(text);
        }

        var obj = new JsonObject();

        foreach (var attribute in attributes)
        {
            obj[attribute.Name.LocalName] = attribute.Value;
        }

        if (children.Count == 0)
        {
            if (!string.IsNullOrEmpty(text))
            {
                obj["#text"] = text;
            }

            return obj;
        }

        foreach (var group in children.GroupBy(c => c.Name.LocalName))
        {
            var members = group.ToList();
            obj[group.Key] = members.Count == 1
                ? ElementToNode(members[0])
                : ToArray(members);
        }

        return obj;
    }

    private static JsonArray ToArray(IReadOnlyList<XElement> elements)
    {
        var array = new JsonArray();
        foreach (var element in elements)
        {
            array.Add(ElementToNode(element));
        }

        return array;
    }

    private static string GetDirectText(XElement element)
    {
        return string.Concat(
            element.Nodes()
                .OfType<XText>()
                .Select(t => t.Value))
            .Trim();
    }
}
