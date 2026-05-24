#include "value.h"
#include "../Compiler/structures.h"

std::unordered_map<std::string, Value>& Value::AsDictionary() const {
    if (type != ValueType::Dictionary || !dict_val) throw std::runtime_error("Not a Dictionary.");
    return *dict_val;
}

std::unordered_map<std::string, Value>& Value::AsModule() const {
    if (type != ValueType::Module || !module_val) throw std::runtime_error("Not a Module.");
    return *module_val;
}

std::string Value::ToString() const {
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

int Chunk::AddConstant(Value value) {
    Constants.push_back(value);
    return Constants.size() - 1;
}