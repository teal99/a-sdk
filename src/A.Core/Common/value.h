#ifndef VALUE_H
#define VALUE_H
#include <string>
#include <vector>
#include <unordered_map>
#include <memory>
#include <stdexcept>
#include <functional>

struct Value;
struct FunctionObject;
struct InstanceObject;

enum class ValueType {
    Nil, Boolean, Number, String, NativeFn, Function, Array, Dictionary, Instance, Module
};

using NativeFunction = std::function<Value(const std::vector<Value>&)>;

struct InstanceObject {
    std::string className;
    std::unordered_map<std::string, Value> fields;
    std::unordered_map<std::string, Value> methods;

    InstanceObject(std::string name, std::unordered_map<std::string, Value> m)
        : className(name), methods(m) {}
};

struct FunctionObject {
    std::string name;
};

struct Value {
    ValueType type;

    union {
        bool boolean;
        double number;
    } as;

    std::string string_val;
    NativeFunction native_fn;
    std::shared_ptr<FunctionObject> function_val;
    std::shared_ptr<std::vector<Value>> array_val;
    std::shared_ptr<std::unordered_map<std::string, Value>> dict_val;
    std::shared_ptr<InstanceObject> instance_val;
    std::shared_ptr<std::unordered_map<std::string, Value>> module_val;

    Value() : type(ValueType::Nil) { as.boolean = false; }
    Value(double val) : type(ValueType::Number) { as.number = val; }
    Value(bool val) : type(ValueType::Boolean) { as.boolean = val; }
    Value(std::string val) : type(ValueType::String), string_val(val) {}
    Value(NativeFunction fn) : type(ValueType::NativeFn), native_fn(fn) {}
    Value(std::shared_ptr<FunctionObject> fn) : type(ValueType::Function), function_val(fn) {}
    Value(std::shared_ptr<std::vector<Value>> arr) : type(ValueType::Array), array_val(arr) {}
    Value(std::shared_ptr<std::unordered_map<std::string, Value>> dict) : type(ValueType::Dictionary), dict_val(dict) {}
    Value(std::shared_ptr<InstanceObject> inst) : type(ValueType::Instance), instance_val(inst) {}

    double AsNumber() const {
        if (type != ValueType::Number) throw std::runtime_error("Value is not a number.");
        return as.number;
    }

    bool AsBoolean() const {
        if (type != ValueType::Boolean) throw std::runtime_error("Value is not a boolean.");
        return as.boolean;
    }

    std::string AsString() const {
        if (type != ValueType::String) throw std::runtime_error("Value is not a string.");
        return string_val;
    }

    std::vector<Value>& AsArray() const {
        if (type != ValueType::Array) throw std::runtime_error("Value is not an Array collection container.");
        return *array_val;
    }

    std::unordered_map<std::string, Value>& AsDictionary() const {
        if (type != ValueType::Dictionary || !dict_val) {
            throw std::runtime_error("Object is not a valid Dictionary.");
        }
        return *dict_val;
    }

    std::unordered_map<std::string, Value>& AsModule() const {
        if (type != ValueType::Module || !module_val) {
            throw std::runtime_error("Object is not a valid Static Module/Namespace.");
        }
        return *module_val;
    }

    InstanceObject& AsInstance() const {
        if (type != ValueType::Instance) throw std::runtime_error("Object is not a valid Class/Struct Instance.");
        return *instance_val;
    }

    std::string ToString() const {
        switch (type) {
            case ValueType::Nil:        return "nil";
            case ValueType::Boolean:    return as.boolean ? "true" : "false";
            case ValueType::Number:     {
                std::string s = std::to_string(as.number);
                s.erase(s.find_last_not_of('0') + 1, std::string::npos);
                if(s.back() == '.') s.pop_back();
                return s;
            }
            case ValueType::String:     return string_val;
            case ValueType::NativeFn:   return "<native fn>";
            case ValueType::Function:   return "<fn " + (function_val ? function_val->Name : "") + ">";
            case ValueType::Array:      return "[Array]";
            case ValueType::Dictionary: return "[Dictionary]";
            case ValueType::Instance:   return "[Instance]";
            case ValueType::Module:     return "[Static Module Namespace]";
            default:                    return "unknown";
        }
    }

    bool is_falsey() const {
        return type == ValueType::Nil || (type == ValueType::Boolean && !as.boolean);
    }
};

#endif