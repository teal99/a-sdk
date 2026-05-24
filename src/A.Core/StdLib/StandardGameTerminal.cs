using System;
using System.Collections.Generic;
using A.Core.Common;

namespace A.Core.StdLib;

public static class GameTerminalModule
{
    public static Value Clear(Value[] _)
    {
        Console.Clear();
        return new Value();
    }

    public static Value DrawBar(Value[] args)
    {
        if (args.Length != 2 || args[0].Type != Common.ValueType.Number || args[1].Type != Common.ValueType.Number)
            throw new ArgumentException("Runtime Error: 'Game.Terminal.DrawBar' expects (current, max).");

        double current = args[0].AsNumber();
        double max = args[1].AsNumber();
        if (max <= 0) max = 1;

        double percent = Math.Clamp(current / max, 0.0, 1.0);
        int width = 20;
        int filled = (int)Math.Round(percent * width);

        string bar = new string('█', filled) + new string('░', width - filled);
        Console.Write($"[{bar}] {Math.Round(percent * 100)}%");
        return new Value();
    }

    public static Value Menu(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.Array)
            throw new ArgumentException("Runtime Error: 'Game.Terminal.Menu' expects an Array of choices.");

        List<Value> choices = args[0].AsArray();
        Console.WriteLine("\n--- SELECT AN OPTION ---");
        for (int i = 0; i < choices.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {choices[i]}");
        }
        Console.WriteLine("------------------------");

        while (true)
        {
            Console.Write("Choice > ");
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int sel) && sel >= 1 && sel <= choices.Count)
            {
                return choices[sel - 1]; // Return selected string choice back to the VM stack
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Invalid choice. Try again.");
            Console.ResetColor();
        }
    }

    public static Value MsgBox(Value[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Runtime Error: 'Game.Terminal.MsgBox' expects (title, message).");
        string title = args[0].ToString();
        string msg = args[1].ToString();

        string border = new string('=', Math.Max(title.Length, msg.Length) + 4);
        Console.WriteLine($"\n{border}");
        Console.WriteLine($"| {title} |");
        Console.WriteLine(border);
        Console.WriteLine($"  {msg}");
        Console.WriteLine($"{border}\n");
        return new Value();
    }
}