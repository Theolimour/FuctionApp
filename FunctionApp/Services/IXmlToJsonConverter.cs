using System.Text.Json.Nodes;

namespace FunctionApp.Services;

public interface IXmlToJsonConverter
{
    JsonArray Convert(string xml);
}
