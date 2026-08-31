using FunctionApp.Services;
using System.Text.Json.Nodes;
using Xunit;

namespace FunctionApp.Tests;

public sealed class XmlToJsonConverterTests
{
    private readonly XmlToJsonConverter _converter = new();

    [Fact]
    public void Convert_RepeatingRootChildren_ReturnsJsonArray()
    {
        const string xml = """
            <Orders>
              <Order id="1"><Customer>Alice</Customer></Order>
              <Order id="2"><Customer>Bob</Customer></Order>
            </Orders>
            """;

        var result = _converter.Convert(xml);

        Assert.Equal(2, result.Count);
        Assert.Equal("1", result[0]!["id"]!.GetValue<string>());
        Assert.Equal("Alice", result[0]!["Customer"]!.GetValue<string>());
        Assert.Equal("Bob", result[1]!["Customer"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_SingleEntity_WrapsObjectInArray()
    {
        const string xml = """
            <Order>
              <Customer>Alice</Customer>
              <Status>Confirmed</Status>
            </Order>
            """;

        var result = _converter.Convert(xml);

        Assert.Single(result);
        Assert.Equal("Alice", result[0]!["Customer"]!.GetValue<string>());
        Assert.Equal("Confirmed", result[0]!["Status"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_XmlFragment_TreatsEachTopLevelElementAsItem()
    {
        const string xml = """
            <Item><Sku>A</Sku></Item>
            <Item><Sku>B</Sku></Item>
            """;

        var result = _converter.Convert(xml);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0]!["Sku"]!.GetValue<string>());
        Assert.Equal("B", result[1]!["Sku"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_NestedRepeatingElements_BecomeJsonArray()
    {
        const string xml = """
            <Orders>
              <Order>
                <Lines>
                  <Line><Sku>A</Sku></Line>
                  <Line><Sku>B</Sku></Line>
                </Lines>
              </Order>
            </Orders>
            """;

        var result = _converter.Convert(xml);

        var lines = Assert.IsType<JsonArray>(result[0]!["Lines"]!["Line"]);
        Assert.Equal(2, lines.Count);
        Assert.Equal("A", lines[0]!["Sku"]!.GetValue<string>());
        Assert.Equal("B", lines[1]!["Sku"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_IgnoresXmlNamespaces()
    {
        const string xml = """
            <Orders xmlns="http://example.com/orders">
              <Order><Customer>Alice</Customer></Order>
            </Orders>
            """;

        var result = _converter.Convert(xml);

        Assert.Single(result);
        Assert.Equal("Alice", result[0]!["Customer"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_LeafWithAttributes_UsesTextProperty()
    {
        const string xml = """
            <Orders>
              <Order>
                <Total currency="USD">10.00</Total>
              </Order>
            </Orders>
            """;

        var result = _converter.Convert(xml);
        var total = Assert.IsType<JsonObject>(result[0]!["Total"]);

        Assert.Equal("USD", total["currency"]!.GetValue<string>());
        Assert.Equal("10.00", total["#text"]!.GetValue<string>());
    }

    [Fact]
    public void Convert_EmptyBody_Throws()
    {
        var ex = Assert.Throws<XmlPayloadException>(() => _converter.Convert("   "));
        Assert.Equal("Request body is empty.", ex.Message);
    }

    [Fact]
    public void Convert_InvalidXml_Throws()
    {
        var ex = Assert.Throws<XmlPayloadException>(() => _converter.Convert("<Order><Customer>Alice</Order>"));
        Assert.Equal("The request body is not valid XML.", ex.Message);
    }
}
