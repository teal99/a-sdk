using System;
using System.IO;
using System.Threading;
using A.Core.Common;

namespace A.Core.StdLib;

public static class FileModule
{
    public static Value Read(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
        {
            throw new ArgumentException("Runtime Error: 'File.Read' expects exactly 1 string argument representing the file path.");
        }

        string path = args[0].AsString();
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Runtime Error: File not found at target directory: '{path}'");
        }

        string content = File.ReadAllText(path);
        return new Value(content);
    }

    public static Value Write(Value[] args)
    {
        if (args.Length != 2 || args[0].Type != Common.ValueType.String || args[1].Type != Common.ValueType.String)
        {
            throw new ArgumentException("Runtime Error: 'File.Write' expects exactly 2 string arguments (filePath, textContent).");
        }

        string path = args[0].AsString();
        string content = args[1].AsString();

        File.WriteAllText(path, content);
        return new Value(); // Returns a Nil variant
    }
        public static Value Exists(Value[] args)
    {
        ValidateArgs("FileSystem.Exists", args, 1, Common.ValueType.String);
        return new Value(File.Exists(args[0].AsString()));
    }

    public static Value Append(Value[] args)
    {
        ValidateArgs("FileSystem.Append", args, 2, Common.ValueType.String, Common.ValueType.String);
        File.AppendAllText(args[0].AsString(), args[1].AsString());
        return new Value(); // Returns Nil
    }

    public static Value Delete(Value[] args)
    {
        ValidateArgs("FileSystem.Delete", args, 1, Common.ValueType.String);
        string path = args[0].AsString();
        if (File.Exists(path)) File.Delete(path);
        return new Value();
    }

    public static Value ReadLines(Value[] args)
    {
        ValidateArgs("FileSystem.ReadLines", args, 1, Common.ValueType.String);
        string path = args[0].AsString();
        if (!File.Exists(path)) throw new FileNotFoundException($"FS Error: File not found at '{path}'");

        string[] lines = File.ReadAllLines(path);
        List<Value> arrayList = new();
        foreach (var line in lines) arrayList.Add(new Value(line));
        return new Value(arrayList); // Returns native Array type primitive
    }

    public static Value WriteLines(Value[] args)
    {
        if (args.Length != 2 || args[0].Type != Common.ValueType.String || args[1].Type != Common.ValueType.Array)
            throw new ArgumentException("Runtime Error: 'FileSystem.WriteLines' expects (string path, array lines).");

        string path = args[0].AsString();
        List<Value> arrayList = args[1].AsArray();
        List<string> lines = new();
        foreach (var val in arrayList) lines.Add(val.ToString());

        File.WriteAllLines(path, lines);
        return new Value();
    }
    private static void ValidateArgs(string name, Value[] args, int expectedCount, params Common.ValueType[] expectedTypes)
    {
        if (args.Length != expectedCount)
            throw new ArgumentException($"Runtime Error: '{name}' expected {expectedCount} arguments, but received {args.Length}.");
        for (int i = 0; i < expectedCount; i++)
            if (args[i].Type != expectedTypes[i])
                throw new InvalidCastException($"Runtime Error: '{name}' argument {i} expected Type {expectedTypes[i]}, but received {args[i].Type}.");
    }
}