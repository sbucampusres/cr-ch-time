namespace CRCHTime.Models;

/// <summary>
/// Represents the result of a stored procedure operation that may return an error message
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static OperationResult Succeeded() => new() { Success = true };
    public static OperationResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
