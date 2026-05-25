using System;
using System.Drawing;
using A.Core.Common;

namespace A.Core.StdLib;

public static class TimeModule
{
    public static Value Now(Value[] _)
    {
        double epochMs = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
        return new Value(epochMs);
    }

    public static Value Format(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
            throw new ArgumentException("Runtime Error: 'Time.Format' expects exactly 1 string parameter format string.");
        return new Value(DateTime.Now.ToString(args[0].AsString()));
    }

    public static Value Sleep(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.Number)
        {
            throw new ArgumentException("Runtime Error: 'Time.Sleep' expects a single numeric argument (milliseconds).");
        }

        int milliseconds = (int)args[0].AsNumber();
        Thread.Sleep(milliseconds);
        return new Value(); 
    }
}