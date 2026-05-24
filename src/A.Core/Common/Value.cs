using System;
using System.Collections.Generic;
using A.Core.Compiler;

namespace A.Core.Common;

public delegate Value NativeFunction(Value[] args);

public enum ValueType
{
    Nil, Boolean, Number, String, NativeFn, Function, Array, Dictionary, Instance, Module
}

public class InstanceObject
{
    public string ClassName { get; }
    public Dictionary<string, Value> Fields { get; } = new();
    public Dictionary<string, Value> Methods { get; }

    public InstanceObject(string className, Dictionary<string, Value> methods)
    {
        ClassName = className;
        Methods = methods;
    }
}

public class Value
{
    public ValueType Type { get; }
    private readonly int _cachedSlotIndex;
    public readonly double _numberValue;
    private readonly bool _boolValue;
    private readonly object? _objValue;
    private readonly NativeFunction? _nativeFnValue;
    private readonly FunctionObject? _functionValue;
    private readonly List<Value>? _arrayValue;

    // --- NEW OBJECT TYPE BACKING FIELDS ---
    private readonly Dictionary<string, Value>? _dictionaryValue;
    private readonly InstanceObject? _instanceValue;
    private readonly Dictionary<string, Value>? _moduleValue;

    // --- Existing Base Constructors ---
    public Value(int fastSlotIndex)
    {
        Type = ValueType.Number; _numberValue = fastSlotIndex; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
        _cachedSlotIndex = fastSlotIndex;
    }

    public Value()
    {
        Type = ValueType.Nil; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(double value)
    {
        Type = ValueType.Number; _numberValue = value; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(bool value)
    {
        Type = ValueType.Boolean; _numberValue = 0; _boolValue = value; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(string value)
    {
        Type = ValueType.String; _numberValue = 0; _boolValue = false;
        _objValue = value.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r");
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(NativeFunction nativeFn)
    {
        Type = ValueType.NativeFn; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = nativeFn; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(FunctionObject function)
    {
        Type = ValueType.Function; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = function; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    public Value(List<Value> array)
    {
        Type = ValueType.Array; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = array; _dictionaryValue = null; _instanceValue = null; _moduleValue = null;
    }

    // --- NEW OBJECT CONSTRUCTORS ---
    public Value(Dictionary<string, Value> dict)
    {
        Type = ValueType.Dictionary; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = dict; _instanceValue = null; _moduleValue = null;
    }

    public Value(InstanceObject instance)
    {
        Type = ValueType.Instance; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = instance; _moduleValue = null;
    }

    public Value(Dictionary<string, Value> moduleDict, bool isModule)
    {
        Type = ValueType.Module; _numberValue = 0; _boolValue = false; _objValue = null;
        _nativeFnValue = null; _functionValue = null; _arrayValue = null; _dictionaryValue = null; _instanceValue = null; _moduleValue = moduleDict;
    }

    // --- Direct Type Extraction Utility Wrappers ---
    public int AsCachedSlot() => _cachedSlotIndex;
    public double AsNumber() => Type == ValueType.Number ? _numberValue : throw new InvalidCastException("Value is not a number.");
    public bool AsBoolean() => Type == ValueType.Boolean ? _boolValue : throw new InvalidCastException("Value is not a boolean.");
    public string AsString() => Type == ValueType.String ? (string)_objValue! : throw new InvalidCastException("Value is not a string.");
    public NativeFunction AsNativeFn() => Type == ValueType.NativeFn ? _nativeFnValue! : throw new InvalidCastException("Value is not a native function.");
    public FunctionObject AsFunction() => Type == ValueType.Function ? _functionValue! : throw new InvalidCastException("Value is not a compiled user function.");
    public List<Value> AsArray() => Type == ValueType.Array ? _arrayValue! : throw new InvalidCastException("Value is not an Array collection container.");
    
    public Dictionary<string, Value> AsDictionary() => _dictionaryValue ?? throw new InvalidCastException("Type Error: Object is not a valid Dictionary.");
    public InstanceObject AsInstance() => _instanceValue ?? throw new InvalidCastException("Type Error: Object is not a valid Class/Struct Instance.");
    public Dictionary<string, Value> AsModule() => _moduleValue ?? throw new InvalidCastException("Type Error: Object is not a valid Static Module/Namespace.");

    // Unified Object String Formatter
    public override string ToString() => Type switch
    {
        ValueType.Nil        => "nil",
        ValueType.Boolean    => _boolValue.ToString().ToLower(),
        ValueType.Number     => _numberValue.ToString(),
        ValueType.String     => (string)_objValue!,
        ValueType.NativeFn   => "<native fn>",
        ValueType.Function   => $"<fn {_functionValue?.Name}>",
        ValueType.Array      => "[" + string.Join(", ", _arrayValue!) + "]",
        ValueType.Dictionary => $"[Dictionary ({_dictionaryValue?.Count ?? 0})]",
        ValueType.Instance   => $"[Instance <{_instanceValue?.ClassName}>]",
        ValueType.Module     => "[Static Module Namespace]",
        _                    => "unknown"
    };
}