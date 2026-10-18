using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnitConverter.Conversion;

namespace UnitConverter.Pages;

public class IndexModel(ConversionService converter) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Category { get; set; } = "length";
    [BindProperty(SupportsGet = true)] public string From { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string To { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Value { get; set; } = "1";

    public ConversionCategory CurrentCategory { get; private set; } = UnitCatalog.Categories[0];
    public ConversionResult? Result { get; private set; }
    public bool StoreResult { get; private set; }
    public string HistoryJson => Result is null ? "" : JsonSerializer.Serialize(new
    {
        category = Result.CategoryId,
        categoryName = CurrentCategory.Name,
        from = Result.FromId,
        to = Result.ToId,
        input = Result.InputText,
        fromSymbol = Result.FromSymbol,
        toSymbol = Result.ToSymbol,
        output = Result.FormattedValue
    });

    public void OnGet()
    {
        PrepareCategory();
        if (string.IsNullOrEmpty(From)) From = CurrentCategory.DefaultFrom;
        if (string.IsNullOrEmpty(To)) To = CurrentCategory.DefaultTo;
        Calculate();
    }

    public void OnPost()
    {
        PrepareCategory();
        Calculate();
        StoreResult = Result is not null;
    }

    public void OnPostSwap()
    {
        PrepareCategory();
        (From, To) = (To, From);
        Calculate();
        StoreResult = Result is not null;
    }

    public string? FieldError(string field) => ModelState.TryGetValue(field, out var state)
        ? state.Errors.FirstOrDefault()?.ErrorMessage : null;

    private void PrepareCategory()
    {
        var category = UnitCatalog.FindCategory(Category);
        if (category is null)
        {
            ModelState.AddModelError(nameof(Category), "Choose a supported conversion category.");
            return;
        }
        CurrentCategory = category;
    }

    private void Calculate()
    {
        if (!ModelState.IsValid) return;
        try
        {
            Result = converter.Convert(Category, From, To, Value);
        }
        catch (ConversionException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
        }
    }
}
