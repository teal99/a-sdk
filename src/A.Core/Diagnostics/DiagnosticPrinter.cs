using System;

namespace A.Core.Diagnostics;

public static class DiagnosticPrinter
{
    public static void Print(AError error)
    {
        Console.ForegroundColor = error.Severity switch
        {
            AErrorSeverity.Fatal => ConsoleColor.DarkRed,
            AErrorSeverity.Error => ConsoleColor.Red,
            AErrorSeverity.Warning => ConsoleColor.Yellow,
            _ => ConsoleColor.Cyan
        };

        string origin = string.IsNullOrEmpty(error.File) ? $"Line {error.Line}" : $"{error.File}:{error.Line}";
        Console.Write($"\n[{error.Severity.ToString().ToUpper()} - {error.Type} Error] ({origin})");
        Console.ResetColor();

        Console.WriteLine($"\n  {error.Message}");

        if (!string.IsNullOrEmpty(error.ExtraInfo))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Context: {error.ExtraInfo}");
            Console.ResetColor();
        }

        if (!string.IsNullOrEmpty(error.Suggestion))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Suggestion: {error.Suggestion}");
            Console.ResetColor();
        }
        Console.WriteLine();
    }
}