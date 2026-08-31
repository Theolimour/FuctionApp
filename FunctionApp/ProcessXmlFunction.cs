using System.Text.Json;
using FunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public sealed class ProcessXmlFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IXmlToJsonConverter _converter;
    private readonly ILogger<ProcessXmlFunction> _logger;

    public ProcessXmlFunction(IXmlToJsonConverter converter, ILogger<ProcessXmlFunction> logger)
    {
        _converter = converter;
        _logger = logger;
    }

    [Function("ProcessXml")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "process-xml")] HttpRequest req)
    {
        string body;
        using (var reader = new StreamReader(req.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        try
        {
            var result = _converter.Convert(body);
            _logger.LogInformation("Converted XML payload to JSON array with {Count} item(s).", result.Count);

            return new ContentResult
            {
                Content = result.ToJsonString(JsonOptions),
                ContentType = "application/json",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (XmlPayloadException ex)
        {
            _logger.LogWarning(ex, "Rejected XML payload.");
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
