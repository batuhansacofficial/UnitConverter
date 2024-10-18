public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
    public string? ErrorMessage { get; set; } // Add an error message property
}