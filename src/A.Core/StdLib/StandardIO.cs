using System;
using System.Drawing;
using A.Core.Common;

namespace A.Core.StdLib;

public static class ConsoleModule
{
    public static Value Print(Value[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            Console.Write(args[i].ToString());
            if (i < args.Length - 1) Console.Write(" ");
        }
        Console.WriteLine();
        return new Value();
    }

    public static Value Write(Value[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            Console.Write(args[i].ToString());
            if (i < args.Length - 1) Console.Write(" ");
        }
        return new Value();
    }

    public static Value Clear(Value[] _)
    {
        Console.Clear();
        return new Value();
    }

    public static Value ReadLine(Value[] _)
    {
        string? input = Console.ReadLine();
        return new Value(input ?? string.Empty);
    }

    public static Value Input(Value[] args) // Write + ReadLine
    {
        Write(args);
        string? input = Console.ReadLine();
        return new Value(input ?? string.Empty);
    }

    public static Value SetColor(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.Number)
        {
            throw new ArgumentException("Runtime Error: 'Console.SetColor' expects a single numeric argument (0-15).");
        }

        int colorCode = (int)args[0].AsNumber();
        if (colorCode >= 0 && colorCode <= 15)
        {
            Console.ForegroundColor = (ConsoleColor)colorCode;
        }
        return new Value();
    }

    public static Value ResetColor(Value[] _)
    {
        Console.ResetColor();
        return new Value();
    }
}