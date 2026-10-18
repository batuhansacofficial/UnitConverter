using UnitConverter.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages().AddMvcOptions(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddSingleton<UnitConverter.Conversion.ConversionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; " +
        "form-action 'self'; frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles();
app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
app.MapConversionApi();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapRazorPages();

app.Run();

public partial class Program;
