using System;
using A.Core.Common;

namespace A.Core.StdLib;

public static class StringModule
{
    public static Value Length(Value[] args)
    {
        ValidateArgs("String.Length", args, 1, Common.ValueType.String);
        return new Value((double)args[0].AsString().Length);
    }

    public static Value Sub(Value[] args)
    {
        if (args.Length != 3) 
            throw new ArgumentException("Runtime Error: 'String.Sub' expects exactly 3 arguments (string, start, length).");
        if (args[0].Type != Common.ValueType.String || args[1].Type != Common.ValueType.Number || args[2].Type != Common.ValueType.Number)
            throw new InvalidCastException("Runtime Error: Invalid argument types passed to 'String.Sub'.");

        string str = args[0].AsString();
        int start = (int)args[1].AsNumber();
        int len = (int)args[2].AsNumber();

        if (start < 0 || start + len > str.Length)
            throw new IndexOutOfRangeException("Runtime Error: 'String.Sub' range bounds exceeded string boundaries.");

        return new Value(str.Substring(start, len));
    }

    public static Value ToNumber(Value[] args)
    {
        ValidateArgs("String.ToNumber", args, 1, Common.ValueType.String);
        string text = args[0].AsString();

        if (double.TryParse(text, out double parsedValue))
        {
            return new Value(parsedValue);
        }
        
        throw new FormatException($"Runtime Error: Cannot cast string text content '{text}' into a numeric value representation.");
    }
    public static Value Split(Value[] args)
    {
        if (args.Length != 2 || args[0].Type != Common.ValueType.String || args[1].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'String.Split' expects (string text, string delimiter).");

        string[] segments = args[0].AsString().Split(new[] { args[1].AsString() }, StringSplitOptions.None);
        List<Value> arrayList = new();
        foreach (var seg in segments) arrayList.Add(new Value(seg));
        return new Value(arrayList);
    }

    public static Value Replace(Value[] args)
    {
        if (args.Length != 3 || args[0].Type != Common.ValueType.String || args[1].Type != Common.ValueType.String || args[2].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'String.Replace' expects (string text, string old, string newValue).");

        return new Value(args[0].AsString().Replace(args[1].AsString(), args[2].AsString()));
    }

    public static Value Trim(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'String.Trim' expects exactly 1 string argument.");
        return new Value(args[0].AsString().Trim());
    }

    public static Value ToLower(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'String.ToLower' expects exactly 1 string argument.");
        return new Value(args[0].AsString().ToLower());
    }

    public static Value ToUpper(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'String.ToUpper' expects exactly 1 string argument.");
        return new Value(args[0].AsString().ToUpper());
    }

    private static void ValidateArgs(string name, Value[] args, int expectedCount, Common.ValueType expectedType)
    {
        if (args.Length != expectedCount)
            throw new ArgumentException($"Runtime Error: Function '{name}' expected {expectedCount} arguments, but received {args.Length}.");
        if (args[0].Type != expectedType)
            throw new InvalidCastException($"Runtime Error: Function '{name}' expected type {expectedType}, but received {args[0].Type}.");
    }
}