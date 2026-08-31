namespace FunctionApp.Services;

public sealed class XmlPayloadException : Exception
{
    public XmlPayloadException(string message) : base(message)
    {
    }

    public XmlPayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
