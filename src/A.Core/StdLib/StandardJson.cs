using System;
using System.Text.Json;
using System.Collections.Generic;
using A.Core.Common;

namespace A.Core.StdLib;

public static class JsonModule
{
    public static Value Parse(Value[] args)
    {
        if (args.Length != 1 || args[0].Type != Common.ValueType.String)
        {
            throw new ArgumentException("Runtime Error: 'Json.Parse' expects exactly 1 string argument.");
        }

        string jsonText = args[0].AsString();
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            return ConvertElement(doc.RootElement);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Runtime Error: Failed to parse invalid JSON content stream: {ex.Message}");
        }
    }

    public static Value Stringify(Value[] args)
    {
        if (args.Length != 1)
        {
            throw new ArgumentException("Runtime Error: 'Json.Stringify' expects exactly 1 argument.");
        }

        return new Value(SerializeValue(args[0]));
    }

    // --- RECURSIVE JSON TO NATIVE TYPE CONVERTER ---
    private static Value ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True   => new Value(true),
            JsonValueKind.False  => new Value(false),
            JsonValueKind.Number => new Value(element.GetDouble()),
            JsonValueKind.String => new Value(element.GetString() ?? ""),
            JsonValueKind.Array  => ConvertJsonArray(element),
            _                    => new Value() // Default fallback to Nil
        };
    }

    private static Value ConvertJsonArray(JsonElement element)
    {
        List<Value> list = new();
        foreach (JsonElement item in element.EnumerateArray())
        {
            list.Add(ConvertElement(item));
        }
        return new Value(list); // Returns your native Array primitive container type
    }

    private static string SerializeValue(Value val)
    {
        return val.Type switch
        {
            Common.ValueType.Nil     => "null",
            Common.ValueType.Boolean => val.AsBoolean().ToString().ToLower(),
            Common.ValueType.String  => $"\"{val.AsString().Replace("\"", "\\\"")}\"",
            Common.ValueType.Number  => val.AsNumber().ToString(),
            Common.ValueType.Array   => SerializeArray(val.AsArray()),
            _                 => "\"<unserializable>\""
        };
    }

    private static string SerializeArray(List<Value> list)
    {
        List<string> parts = new();
        foreach (var item in list) parts.Add(SerializeValue(item));
        return "[" + string.Join(",", parts) + "]";
    }
}