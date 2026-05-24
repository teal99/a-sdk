#ifndef VALUE_H
#define VALUE_H
#include <string>
#include <vector>
#include <unordered_map>
#include <memory>
#include <stdexcept>
#include <functional>
#include "../Compiler/structures.h"

struct FunctionObject;
class InstanceObject;

enum class ValueType {
    Nil, Boolean, Number, String, NativeFn, Function, Array, Dictionary, Instance, Module
};

struct Value;
using NativeFunction = std::function<Value(const std::vector<Value>&)>;

class InstanceObject {
public:
    std::string ClassName;
    std::unordered_map<std::string, Value> Fields;
    std::unordered_map<std::string, Value> Methods;

    InstanceObject(std::string className, std::unordered_map<std::string, Value> methods)
        : ClassName(className), Methods(methods) {}
};

struct Value {
public:
    ValueType Type;
    
private:
    int _cachedSlotIndex = 0;

public:
    double _numberValue = 0.0;
    bool _boolValue = false;
    std::string _objValue = ""; // Holds standard String values safely
    
    NativeFunction _nativeFnValue;
    std::shared_ptr<FunctionObject> _functionValue;
    std::shared_ptr<std::vector<Value>> _arrayValue;

    // --- NEW OBJECT TYPE BACKING FIELDS ---
    std::shared_ptr<std::unordered_map<std::string, Value>> _dictionaryValue;
    std::shared_ptr<InstanceObject> _instanceValue;
    std::shared_ptr<std::unordered_map<std::string, Value>> _moduleValue;

    // --- Existing Base Constructors ---
    Value(int fastSlotIndex)
        : Type(ValueType::Number), _cachedSlotIndex(fastSlotIndex), _numberValue(fastSlotIndex) {}

    Value()
        : Type(ValueType::Nil) {}

    Value(double value)
        : Type(ValueType::Number), _numberValue(value) {}

    Value(bool value)
        : Type(ValueType::Boolean), _boolValue(value) {}

    Value(std::string value)
        : Type(ValueType::String), _objValue(value) {}

    Value(const char* value)
        : Type(ValueType::String), _objValue(value ? value : "") {}

    Value(NativeFunction nativeFn)
        : Type(ValueType::NativeFn), _nativeFnValue(nativeFn) {}

    Value(std::shared_ptr<FunctionObject> function)
        : Type(ValueType::Function), _functionValue(function) {}

    Value(std::shared_ptr<std::vector<Value>> array)
        : Type(ValueType::Array), _arrayValue(array) {}

    // --- NEW OBJECT CONSTRUCTORS ---
    Value(std::shared_ptr<std::unordered_map<std::string, Value>> dict)
        : Type(ValueType::Dictionary), _dictionaryValue(dict) {}

    Value(std::shared_ptr<InstanceObject> instance)
        : Type(ValueType::Instance), _instanceValue(instance) {}

    Value(std::shared_ptr<std::unordered_map<std::string, Value>> moduleDict, bool isModule)
        : Type(isModule ? ValueType::Module : ValueType::Module), _moduleValue(moduleDict) {}

    // --- Direct Type Extraction Utility Wrappers ---
    int AsCachedSlot() const { return _cachedSlotIndex; }
    
    double AsNumber() const {
        if (Type != ValueType::Number) throw std::runtime_error("Value is not a number.");
        return _numberValue;
    }

    bool AsBoolean() const {
        if (Type != ValueType::Boolean) throw std::runtime_error("Value is not a boolean.");
        return _boolValue;
    }

    std::string AsString() const {
        if (Type != ValueType::String) throw std::runtime_error("Value is not a string.");
        return _objValue;
    }

    NativeFunction AsNativeFn() const {
        if (Type != ValueType::NativeFn) throw std::runtime_error("Value is not a native function.");
        return _nativeFnValue;
    }

    std::shared_ptr<FunctionObject> AsFunction() const {
        if (Type != ValueType::Function) throw std::runtime_error("Value is not a compiled user function.");
        return _functionValue;
    }

    std::vector<Value>& AsArray() const {
        if (Type != ValueType::Array || !_arrayValue) throw std::runtime_error("Value is not an Array collection container.");
        return *_arrayValue;
    }
    
    std::unordered_map<std::string, Value>& AsDictionary() const;
    InstanceObject& AsInstance() const;
    std::unordered_map<std::string, Value>& AsModule() const;

    bool is_falsey() const {
        return Type == ValueType::Nil || (Type == ValueType::Boolean && !_boolValue);
    }

    std::string ToString() const;
};

#endif