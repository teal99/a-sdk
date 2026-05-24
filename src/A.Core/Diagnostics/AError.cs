namespace A.Core.Diagnostics;

public struct AError
{
    public string Type { get; set; }        // e.g., "Syntax", "Compile", "Runtime"
    public string Message { get; set; }     // Core description
    public string? ExtraInfo { get; set; }   // Contextual code snippet or detail
    public string? Suggestion { get; set; }  // "Did you mean...?" / "Expected '{'..."
    public int Line { get; set; }
    public string? File { get; set; }
    public AErrorSeverity Severity { get; set; }
}