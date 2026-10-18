using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace UnitConverter.Tests;

public sealed class ConversionApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    [Fact]
    public async Task Renders_the_default_conversion_and_all_five_category_links()
    {
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("3.28084", ResultValue(html));
        Assert.Equal("m", SelectedUnit(html, "From"));
        Assert.Equal("ft", SelectedUnit(html, "To"));
        var categories = Regex.Matches(html, """\bdata-category\s*=\s*["'](?<id>[^"']+)["']""")
            .Select(match => match.Groups["id"].Value).ToArray();
        Assert.Equal(new[] { "length", "weight", "temperature", "area", "volume" }, categories);
    }

    [Fact]
    public async Task Posts_a_native_form_with_an_antiforgery_cookie_and_converts_temperature()
    {
        var html = await client.GetStringAsync("/?Category=temperature", TestContext.Current.CancellationToken);
        var form = ConversionForm("temperature", "c", "f", "20", AntiforgeryToken(html));
        var response = await client.PostAsync("/", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var converted = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("68", ResultValue(converted));
        Assert.Equal("c", SelectedUnit(converted, "From"));
        Assert.Equal("f", SelectedUnit(converted, "To"));
        Assert.Contains("data-store=\"true\"", converted);
    }

    [Fact]
    public async Task Swaps_units_through_the_native_form_and_recalculates_the_current_input()
    {
        var html = await client.GetStringAsync("/?Category=temperature&From=c&To=f&Value=68", TestContext.Current.CancellationToken);
        var form = ConversionForm("temperature", "c", "f", "68", AntiforgeryToken(html));
        var response = await client.PostAsync("/?handler=Swap", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var swapped = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("f", SelectedUnit(swapped, "From"));
        Assert.Equal("c", SelectedUnit(swapped, "To"));
        Assert.Equal("68", Attribute(InputTag(swapped, "Value"), "value"));
        Assert.Equal("20", ResultValue(swapped));
    }

    [Fact]
    public async Task Invalid_form_input_renders_a_field_error_and_removes_the_previous_result()
    {
        var html = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal("3.28084", ResultValue(html));
        var form = ConversionForm("length", "m", "ft", "not-a-number", AntiforgeryToken(html));
        var response = await client.PostAsync("/", new FormUrlEncodedContent(form), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invalid = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var valueInput = InputTag(invalid, "Value");
        Assert.Equal("true", Attribute(valueInput, "aria-invalid"));
        Assert.Contains("value-error", Attribute(valueInput, "aria-describedby").Split(' '));
        Assert.Equal("not-a-number", Attribute(valueInput, "value"));
        Assert.Matches("""<div\b[^>]*role="alert"[^>]*data-error-summary""", invalid);
        var fieldError = Regex.Match(invalid, """<span\b[^>]*id="value-error"[^>]*>(?<error>.*?)</span>""", RegexOptions.Singleline);
        Assert.True(fieldError.Success, "Expected the value field's error element.");
        Assert.Contains("Enter a number", WebUtility.HtmlDecode(fieldError.Groups["error"].Value));
        Assert.DoesNotContain("data-result-value", invalid);
        Assert.Contains("result-empty", invalid);
    }

    [Fact]
    public async Task Rejects_a_native_form_post_without_an_antiforgery_token()
    {
        await client.GetAsync("/", TestContext.Current.CancellationToken);
        var response = await client.PostAsync("/", new FormUrlEncodedContent(
            ConversionForm("temperature", "c", "f", "20")), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_returns_a_successful_json_status()
    {
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("healthy", result.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Lists_categories_with_unit_ids_and_defaults_without_internal_conversion_constants()
    {
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/categories", TestContext.Current.CancellationToken);
        Assert.Equal(5, categories.GetArrayLength());
        foreach (var category in categories.EnumerateArray())
        {
            var units = category.GetProperty("units").EnumerateArray().ToArray();
            Assert.Contains(units, unit => unit.GetProperty("id").GetString() == category.GetProperty("defaultFrom").GetString());
            Assert.Contains(units, unit => unit.GetProperty("id").GetString() == category.GetProperty("defaultTo").GetString());
            Assert.All(units, unit =>
            {
                Assert.False(unit.TryGetProperty("scale", out _));
                Assert.False(unit.TryGetProperty("offset", out _));
                Assert.False(string.IsNullOrWhiteSpace(unit.GetProperty("symbol").GetString()));
            });
        }
    }

    [Fact]
    public async Task Converts_with_default_precision_and_preserves_numeric_values_as_strings()
    {
        var response = await client.PostAsJsonAsync("/api/convert", new { category = "length", from = "mi", to = "km", value = "1" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("1", result.GetProperty("inputValue").GetString());
        Assert.Equal("1.609344", result.GetProperty("value").GetString());
        Assert.Equal("1.609344", result.GetProperty("formattedValue").GetString());
        Assert.False(result.GetProperty("isRounded").GetBoolean());
    }

    [Fact]
    public async Task Preserves_all_digits_at_the_decimal_maximum()
    {
        const string maximum = "79228162514264337593543950335";
        var response = await client.PostAsJsonAsync("/api/convert", new { category = "length", from = "m", to = "m", value = maximum }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(maximum, result.GetProperty("value").GetString());
    }

    [Fact]
    public async Task Converts_temperature_and_applies_requested_display_precision()
    {
        var response = await client.PostAsJsonAsync("/api/convert", new { category = "temperature", from = "f", to = "c", value = "98.6", precision = 2 }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("37", result.GetProperty("value").GetString());
        Assert.Equal("37", result.GetProperty("formattedValue").GetString());
    }

    [Theory]
    [InlineData("{}", "Category")]
    [InlineData("{\"category\":null,\"from\":null,\"to\":null,\"value\":null}", "Category")]
    [InlineData("{\"category\":\"length\"}", "From")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\"}", "To")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\",\"to\":\"cm\"}", "Value")]
    [InlineData("{\"category\":\"temperature\",\"from\":\"k\",\"to\":\"c\",\"value\":\"-1\"}", "Value")]
    [InlineData("{\"category\":\"length\",\"from\":\"kg\",\"to\":\"m\",\"value\":\"1\"}", "From")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\",\"to\":\"cm\",\"value\":\"1,25\"}", "Value")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\",\"to\":\"cm\",\"value\":\"1\",\"precision\":13}", "Precision")]
    public async Task Returns_field_validation_as_problem_details(string json, string field)
    {
        var response = await client.PostAsync("/api/convert", new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out var messages));
        Assert.NotEmpty(messages.EnumerateArray());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("{\"category\":1}")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\",\"to\":\"cm\",\"value\":1}")]
    [InlineData("{\"category\":\"length\",\"from\":\"m\",\"to\":\"cm\",\"value\":\"1\",\"precision\":null}")]
    public async Task Returns_safe_body_validation_for_malformed_or_wrongly_typed_json(string json)
    {
        var response = await client.PostAsync("/api/convert", new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(problem.GetProperty("errors").TryGetProperty("Body", out _));
        Assert.DoesNotContain("Exception", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Rejects_non_json_content_with_a_problem_response()
    {
        var response = await client.PostAsync("/api/convert", new StringContent("value=1", Encoding.UTF8, "text/plain"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(415, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").TryGetProperty("Body", out _));
    }

    [Fact]
    public async Task Serves_the_openapi_contract_for_the_real_routes_and_exact_numeric_strings()
    {
        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("3.0.3", document.GetProperty("openapi").GetString());
        Assert.True(document.GetProperty("paths").GetProperty("/api/categories").TryGetProperty("get", out _));
        Assert.True(document.GetProperty("paths").GetProperty("/api/convert").TryGetProperty("post", out _));
        Assert.Equal("string", document.GetProperty("components").GetProperty("schemas")
            .GetProperty("ConversionResponse").GetProperty("properties").GetProperty("value").GetProperty("type").GetString());
    }

    private static Dictionary<string, string> ConversionForm(
        string category, string from, string to, string value, string? token = null)
    {
        var form = new Dictionary<string, string>
        {
            ["Category"] = category,
            ["From"] = from,
            ["To"] = to,
            ["Value"] = value
        };
        if (token is not null) form["__RequestVerificationToken"] = token;
        return form;
    }

    private static string AntiforgeryToken(string html)
    {
        var token = Attribute(InputTag(html, "__RequestVerificationToken"), "value");
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private static string InputTag(string html, string name)
    {
        var input = Regex.Matches(html, """<input\b[^>]*>""", RegexOptions.IgnoreCase)
            .SingleOrDefault(match => Attribute(match.Value, "name") == name);
        Assert.NotNull(input);
        return input.Value;
    }

    private static string SelectedUnit(string html, string name)
    {
        var select = Regex.Match(html,
            $"""<select\b[^>]*id="{Regex.Escape(name)}"[^>]*>(?<options>.*?)</select>""", RegexOptions.Singleline);
        Assert.True(select.Success, $"Expected the {name} unit selector.");
        var selected = Regex.Matches(select.Groups["options"].Value, """<option\b[^>]*>""")
            .SingleOrDefault(match => Regex.IsMatch(match.Value, """\sselected(?:\s*=|\s|>)"""));
        Assert.NotNull(selected);
        return Attribute(selected.Value, "value");
    }

    private static string Attribute(string tag, string name)
    {
        var match = Regex.Match(tag, $"""\s{Regex.Escape(name)}\s*=\s*["'](?<value>.*?)["']""", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value) : "";
    }

    private static string ResultValue(string html)
    {
        var match = Regex.Match(html, """<span\b[^>]*\bdata-result-value[^>]*>(?<value>.*?)</span>""", RegexOptions.Singleline);
        Assert.True(match.Success, "Expected a rendered conversion result.");
        return WebUtility.HtmlDecode(match.Groups["value"].Value.Trim());
    }

    public void Dispose() => client.Dispose();
}
