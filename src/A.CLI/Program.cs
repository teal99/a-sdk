using A.Core.Lexer;
using A.Core.Parser;
using A.Core.VM;
using A.Core.Diagnostics;
using A.Core.Common;

namespace A.CLI;

internal static class Program
{
    private static readonly VM _vm = new();

    static void Main(string[] args)
    {
        Console.Title = "A Language SDK Environment";

        if (args.Length > 1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Usage Error: a-cli [script.a]");
            Console.ResetColor();
            Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    private static void RunFile(string path)
    {
        if (!File.Exists(path))
        {
            var error = new AError
            {
                Type = "IO Target",
                Message = $"The specified script source document could not be discovered.",
                Suggestion = $"Verify that the absolute path file target exists at: '{path}'",
                Line = 0,
                File = path,
                Severity = AErrorSeverity.Fatal
            };
            DiagnosticPrinter.Print(error);
            Environment.Exit(66);
        }

        string source = File.ReadAllText(path);
        
        Run(source, isReplMode: false);
    }

    private static void RunPrompt()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=================================================");
        Console.WriteLine("A Language Interactive REPL Shell [v0.1.0, Unstable]");
        Console.WriteLine("     State-Preserving Virtual Machine Engine     ");
        Console.WriteLine("=================================================");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Type statements and press Enter. Type 'exit' to quit.\n");
        Console.ResetColor();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("a> ");
            Console.ResetColor();

            string? line = Console.ReadLine();
            if (line == null || line.Trim() == "exit") break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            Run(line, isReplMode: true);
        }
    }

    private static void Run(string source, bool isReplMode)
    {
        Lexer lexer = new Lexer(source);
        var tokens = lexer.ScanTokens();

        var parser = new Parser(tokens);
        try
        {
            var chunk = parser.Compile();

            if (parser.HadError)
            {
                return;
            }

            bool isDevelopment = Config.Environment.ToLower().Trim() == "development";
            if (isDevelopment || Config.RelayOpcodes)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n--- DEBUG BYTECODE DISASSEMBLY TRACE ---");
                Console.ResetColor();

                int offset = 0;
                while (offset < chunk.Code.Count)
                {
                    offset = Disassembler.DisassembleInstruction(chunk, offset);
                    
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("----------------------------------------\n");
                    Console.ResetColor();
                }
            }

            _vm.Interpret(chunk, isReplMode);
            
            if (_vm.LastRuntimeError.HasValue)
            {
                DiagnosticPrinter.Print(_vm.LastRuntimeError.Value);
            }
            
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"[Fatal Engine Panic] Internal Pipeline Exception Crash: {ex.Message}");
            Console.ResetColor();
        }
    }
}