using System;
using A.Core.Common;

namespace A.Core.StdLib;

public static class CollectionsModule
{
    public static Value Length(Value[] args)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("Runtime Error: Function 'Array.Length' expected exactly 1 array argument.");
        }
        
        if (args[0].Type != Common.ValueType.Array)
        {
            throw new InvalidCastException($"Runtime Error: Function 'Array.Length' expected an Array type container, but received Type '{args[0].Type}'.");
        }

        return new Value(args[0].AsArray().Count);
    }
}