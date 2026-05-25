using System;
using A.Core.Common;

namespace A.Core.StdLib;

public static class MathModule
{
    public static Value Abs(Value[] args)
    {
        ValidateArgCount("math.abs", args, 1);
        double num = ValidateNumericArg("math.abs", args[0]);
        return new Value(Math.Abs(num));
    }

    public static Value Sqrt(Value[] args)
    {
        ValidateArgCount("math.sqrt", args, 1);
        double num = ValidateNumericArg("math.sqrt", args[0]);
        if (num < 0) throw new InvalidOperationException("Runtime Error: Cannot calculate the square root of a negative number.");
        return new Value(Math.Sqrt(num));
    }

    public static Value Pow(Value[] args)
    {
        ValidateArgCount("math.pow", args, 2);
        double baseNum = ValidateNumericArg("math.pow", args[0]);
        double exponent = ValidateNumericArg("math.pow", args[1]);
        return new Value(Math.Pow(baseNum, exponent));
    }

    public static Value Round(Value[] args)
    {
        ValidateArgCount("math.round", args, 1);
        double num = ValidateNumericArg("math.round", args[0]);
        return new Value(Math.Round(num));
    }

    public static Value Floor(Value[] args)
    {
        ValidateArgCount("math.floor", args, 1);
        double num = ValidateNumericArg("math.floor", args[0]);
        return new Value(Math.Floor(num));
    }

    public static Value Ceil(Value[] args)
    {
        ValidateArgCount("math.ceil", args, 1);
        double num = ValidateNumericArg("math.ceil", args[0]);
        return new Value(Math.Ceiling(num));
    }

    private static void ValidateArgCount(string functionName, Value[] args, int expected)
    {
        if (args.Length != expected)
        {
            throw new ArgumentException($"Runtime Error: Function '{functionName}' expected {expected} arguments, but received {args.Length}.");
        }
    }

    private static double ValidateNumericArg(string functionName, Value arg)
    {
        if (arg.Type != Common.ValueType.Number)
        {
            throw new InvalidCastException($"Runtime Error: Function '{functionName}' expected a numeric argument, but received Type '{arg.Type}'.");
        }
        return arg.AsNumber();
    }
}