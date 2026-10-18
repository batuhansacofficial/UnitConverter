using System.Globalization;
using System.Text.Json;
using UnitConverter.Conversion;

namespace UnitConverter.Api;

public static class ConversionEndpoints
{
    public static IEndpointRouteBuilder MapConversionApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/categories", () => UnitCatalog.Categories.Select(category => new
        {
            category.Id,
            category.Name,
            category.Description,
            category.DefaultFrom,
            category.DefaultTo,
            Units = category.Units.Select(unit => new { unit.Id, unit.Name, unit.Symbol })
        })).WithName("GetConversionCategories");

        endpoints.MapPost("/api/convert", ConvertAsync).WithName("ConvertMeasurement");

        endpoints.MapGet("/openapi/v1.json", (IWebHostEnvironment environment) =>
            Results.File(Path.Combine(environment.ContentRootPath, "wwwroot", "openapi.json"), "application/json"))
            .WithName("GetOpenApiDocument");

        return endpoints;
    }

    private static async Task<IResult> ConvertAsync(
        HttpRequest request,
        ConversionService service,
        CancellationToken cancellationToken)
    {
        if (!request.HasJsonContentType())
            return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType,
                title: "Unsupported media type.",
                detail: "Send the request with Content-Type: application/json.",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = new Dictionary<string, string[]> { ["Body"] = ["A JSON request body is required."] }
                });

        ConversionRequest? input;
        try
        {
            input = await request.ReadFromJsonAsync<ConversionRequest>(cancellationToken);
        }
        catch (JsonException)
        {
            return InvalidBody("Send a JSON object with category, from, to, and value as strings, and precision as an integer.");
        }
        catch (BadHttpRequestException)
        {
            return InvalidBody("The request body could not be read. Send a valid JSON object.");
        }

        if (input is null)
            return InvalidBody("A JSON object is required.");

        try
        {
            var result = service.Convert(input.Category ?? "", input.From ?? "", input.To ?? "", input.Value ?? "", input.Precision);
            return Results.Ok(new ConversionResponse(
                result.CategoryId,
                result.FromId,
                result.ToId,
                result.InputValue.ToString("G29", CultureInfo.InvariantCulture),
                result.Value.ToString("G29", CultureInfo.InvariantCulture),
                result.FormattedValue,
                result.InputText,
                result.FromName,
                result.ToName,
                result.FromSymbol,
                result.ToSymbol,
                result.Formula,
                result.IsRounded));
        }
        catch (ConversionException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [exception.Field] = [exception.Message] },
                title: "Check the conversion request.");
        }
    }

    private static IResult InvalidBody(string message) => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["Body"] = [message] },
        title: "Check the conversion request.");
}

public sealed record ConversionRequest(string? Category, string? From, string? To, string? Value, int Precision = 6);

/// <summary>Numeric strings retain decimal precision in clients that use binary floating-point numbers.</summary>
public sealed record ConversionResponse(
    string CategoryId,
    string FromId,
    string ToId,
    string InputValue,
    string Value,
    string FormattedValue,
    string InputText,
    string FromName,
    string ToName,
    string FromSymbol,
    string ToSymbol,
    string Formula,
    bool IsRounded);
