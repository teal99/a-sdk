#include "vm.h"
#include <iostream>
#include <algorithm>

const std::unordered_map<std::string, std::function<Value(const std::vector<Value>&)>> VM::_stringPrototype = {
    { "Length",  [](const std::vector<Value>& args) { return Value((double)args[0].AsString().length()); } },
    { "ToUpper", [](const std::vector<Value>& args) { return Value(args[0].AsString()); } },
    { "ToLower", [](const std::vector<Value>& args) { return Value(args[0].AsString()); } }
};

const std::unordered_map<std::string, std::function<Value(const std::vector<Value>&)>> VM::_arrayPrototype = {
    { "Length",  [](const std::vector<Value>& args) { return Value((double)args[0].AsArray().size()); } }
};

VM::VM() {
    _globalCount = 0;
}

// --- VM STACK UTILITIES ---
void VM::Push(Value value) {
    if (_stackTop >= 2048) {
        int frameIndex = _frameCount - 1;
        int activeIp = frameIndex >= 0 ? _frames[frameIndex].ip : 0;
        OpCode leakingOp = (frameIndex >= 0 && activeIp > 0) 
            ? static_cast<OpCode>(_frames[frameIndex].function->chunk.Code[activeIp - 1]) 
            : OpCode::Return;

        // FIXED FIELD ORDER: Type, Message, ExtraInfo, Suggestion, Line, File, Severity
        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
            "Stack Overflow",
            "Virtual Machine Value Stack exceeded allocation window limits (Size 2048) at OpCode: '" + std::to_string(static_cast<int>(leakingOp)) + "'",
            "",
            "The statement loop body is leaking expression values. Ensure your block parser emits an explicit Pop instruction at line breaks.",
            activeIp,
            "main.a",
            A::Core::Diagnostics::AErrorSeverity::Fatal
        });
        return;
    }
    _stack[_stackTop++] = value;
}

Value VM::Pop() {
    if (_stackTop == 0) throw std::runtime_error("VM stack underflow.");
    return _stack[--_stackTop];
}

Value VM::Peek(int distance) {
    return _stack[_stackTop - 1 - distance];
}

void VM::BinaryOp(std::function<Value(double, double)> operation) {
    if (Peek(0).type == ValueType::String || Peek(1).type == ValueType::String) {
        Value bVal = Pop();
        Value aVal = Pop();
        Push(Value(aVal.ToString() + bVal.ToString()));
        return;
    }

    if (Peek(0).type != ValueType::Number || Peek(1).type != ValueType::Number) {
        int activeFrameIndex = _frameCount - 1;
        
        // FIXED FIELD ORDER: Type, Message, ExtraInfo, Suggestion, Line, File, Severity
        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
            "Runtime Math",
            "Operands must be valid numbers for arithmetic calculations.",
            "",
            "Check your stack values. You attempted to process a calculation using bad types.",
            (_frames[activeFrameIndex].function->chunk.Code.size() > 0) ? _frames[activeFrameIndex].ip : 1,
            "main.a",
            A::Core::Diagnostics::AErrorSeverity::Fatal
        });
        return;
    }

    double b = Pop().AsNumber();
    double a = Pop().AsNumber();
    Push(operation(a, b));
}

void VM::CompareOp(std::function<bool(double, double)> operation) {
    if (Peek(0).type != ValueType::Number || Peek(1).type != ValueType::Number) {
        throw std::runtime_error("Runtime Error: Operands must be numbers.");
    }
    double b = Pop().AsNumber();
    double a = Pop().AsNumber();
    Push(Value(operation(a, b)));
}

// --- BYTE NAVIGATION UTILITIES ---
CallFrame& VM::CurrentFrame() {
    return _frames[_frameCount - 1];
}

uint8_t VM::ReadByte() {
    int activeFrameIndex = _frameCount - 1;
    return _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];
}

Value VM::ReadConstant() {
    uint8_t index = ReadByte();
    return CurrentFrame().function->chunk.Constants[index];
}

uint16_t VM::ReadShort() {
    int activeFrameIndex = _frameCount - 1;
    uint16_t offset = static_cast<uint16_t>(_frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++] << 8);
    offset |= _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];
    return offset;
}

// --- EXTRA HELPERS ---
bool VM::IsFalsey(Value value) {
    if (value.type == ValueType::Nil) return true;
    if (value.type == ValueType::Boolean) return !value.AsBoolean();
    return false;
}

VM::InterpretResult VM::Interpret(Chunk chunk, bool isReplMode) {
    LastRuntimeError = nullptr;

    if (!isReplMode) {
        _stackTop = 0;
        for (int i = _globalCount; i < 256; i++) {
            _globalSlots[i] = Value();
        }
    }

    auto scriptMain = std::make_shared<FunctionObject>(isReplMode ? "repl_line": "main");
    scriptMain->chunk.Code = chunk.Code;
    scriptMain->chunk.Constants = chunk.Constants;

    _frameCount = 0;
    int slotStart = _stackTop;

    Value scriptMainValue(scriptMain);
    Push(scriptMainValue);

    _frames[_frameCount++] = CallFrame(scriptMain, slotStart);

    return Run();
}

VM::InterpretResult VM::Run() {
    while (_frameCount > 0) {
        if (_frameCount - 1 < 0 || _frameCount - 1 >= 1024) {
            // FIXED FIELD ORDER: Type, Message, ExtraInfo, Suggestion, Line, File, Severity
            LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                "Call Stack",
                "Runtime Fatal Error: Virtual Machine Call Stack Frame index out of bounds tracking limits.",
                "",
                "Check for deep or infinite recursive call chains inside your user functions loops.",
                1,
                "main.a",
                A::Core::Diagnostics::AErrorSeverity::Fatal
            });
            return InterpretResult::RuntimeError;
        }
        
        int activeFrameIndex = _frameCount - 1;
        CallFrame& frame = _frames[activeFrameIndex];

        if (frame.ip < 0 || frame.ip >= (int)frame.function->chunk.Code.size()) {
            // FIXED FIELD ORDER: Type, Message, ExtraInfo, Suggestion, Line, File, Severity
            LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                "Runtime Execution",
                "Runtime Fatal Error: Instruction Pointer jumped out of active bytecode bounds.",
                "",
                "Ensure loop boundaries or conditional control branches backpatch cleanly.",
                frame.ip,
                "main.a",
                A::Core::Diagnostics::AErrorSeverity::Fatal
            });
            return InterpretResult::RuntimeError;
        }

        OpCode instruction = static_cast<OpCode>(_frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++]);
        
        switch (instruction) {
            case OpCode::Constant: {
                Push(ReadConstant());
                break;
            }
            
            case OpCode::Add:       BinaryOp([](double a, double b) { return Value(a + b); }); break;
            case OpCode::Subtract:  BinaryOp([](double a, double b) { return Value(a - b); }); break;
            case OpCode::Multiply:  BinaryOp([](double a, double b) { return Value(a * b); }); break;
            case OpCode::Divide:    BinaryOp([](double a, double b) { return Value(a / b); }); break;
            case OpCode::Modulo:    BinaryOp([](double a, double b) { return Value((double)((int)a % (int)b)); }); break;
            
            case OpCode::Negate: {
                if (Peek(0).type != ValueType::Number) {
                    // FIXED FIELD ORDER: Type, Message, ExtraInfo, Suggestion, Line, File, Severity
                    LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                        "Runtime Math",
                        "Operands must be valid numbers for arithmetic calculations.",
                        "",
                        "Check your stack values.",
                        _frames[activeFrameIndex].ip,
                        "main.a",
                        A::Core::Diagnostics::AErrorSeverity::Fatal
                    });
                    return InterpretResult::RuntimeError;
                }
                Push(Value(-Pop().AsNumber()));
                break;
            }
                
            case OpCode::DefineGlobal: {
                uint8_t slotIndex = ReadByte();
                _globalSlots[slotIndex] = Pop();
                break;
            }

            case OpCode::GetGlobal: {
                uint8_t slotIndex = ReadByte();
                Push(_globalSlots[slotIndex]); 
                break;
            }
            
            case OpCode::AssignGlobal:
            case OpCode::GetGlobalFast:
            case OpCode::AssignGlobalFast: {
                uint8_t slotIndex = ReadByte();
                _globalSlots[slotIndex] = Peek(0);
                break;
            }
            
            case OpCode::True:  Push(Value(true)); break;
            case OpCode::False: Push(Value(false)); break;
            
            case OpCode::Equal: {
                Value b = Pop();
                Value a = Pop();
                Push(Value(a.type == b.type && a.ToString() == b.ToString())); 
                break;
            }
            
            case OpCode::Greater:      CompareOp([](double a, double b) { return a > b; }); break;
            case OpCode::Lesser:       CompareOp([](double a, double b) { return a < b; }); break;
            case OpCode::GreaterEqual: CompareOp([](double a, double b) { return a >= b; }); break;
            case OpCode::LesserEqual:  CompareOp([](double a, double b) { return a <= b; }); break;
            case OpCode::EqualEqual: {
                Value b = Pop();
                Value a = Pop();
                bool areEqual = (a.type == b.type && a.ToString() == b.ToString());
                Push(Value(areEqual));
                break;
            }   
            case OpCode::UnEqual: {
                Value b = Pop();
                Value a = Pop();
                bool areNotEqual = !(a.type == b.type && a.ToString() == b.ToString());
                Push(Value(areNotEqual));
                break;
            }

            case OpCode::Call: {
                uint8_t argCount = ReadByte();
                // Look beneath the arguments to find the function object
                Value callee = _stack[_stackTop - argCount - 1];
                
                if (callee.type == ValueType::NativeFn) {
                    std::vector<Value> args(argCount);
                    for (int i = argCount - 1; i >= 0; i--) {
                        args[i] = Pop();
                    }
                    Pop(); // Discard the native function identifier from the stack
                    Value result = callee.native_fn(args);
                    Push(result);
                }
                else if (callee.type == ValueType::Function) {
                    std::shared_ptr<FunctionObject> function = callee.function_val;
                    
                    if (argCount != function->Arity) {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Runtime",
                            "Expected " + std::to_string(function->Arity) + " arguments, but got " + std::to_string(argCount) + ".",
                            "",
                            "Check the number of arguments passed to the function.",
                            (_frames[_frameCount - 1].function->chunk.Code.size() > 0) ? _frames[_frameCount - 1].ip : 1,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });
                        return InterpretResult::RuntimeError;
                    }

                    if (_frameCount >= 1024) {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Runtime",
                            "Stack Overflow.",
                            "",
                            "Check for infinite recursion or deeply nested function calls.",
                            (_frames[_frameCount - 1].function->chunk.Code.size() > 0) ? _frames[_frameCount - 1].ip : 1,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });
                        return InterpretResult::RuntimeError;
                    }

                    int slotsStart = _stackTop - argCount - 1;
                    _frames[_frameCount++] = CallFrame(function, slotsStart);
                }
                else {
                    LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                        "Runtime",
                        "Runtime Error: Can only call functions.",
                        "",
                        "Ensure you are calling a valid function.",
                        (_frames[_frameCount - 1].function->chunk.Code.size() > 0) ? _frames[_frameCount - 1].ip : 1,
                        "main.a",
                        A::Core::Diagnostics::AErrorSeverity::Fatal
                    });
                    return InterpretResult::RuntimeError;
                }
                break;
            }

            case OpCode::JumpIfFalse: {
                uint16_t offset = ReadShort();
                Value condition = Pop();
                bool shouldJump = condition.type == ValueType::Nil || (condition.type == ValueType::Boolean && !condition.AsBoolean());
                if (shouldJump) {
                    _frames[_frameCount - 1].ip += offset;
                }
                break;
            }
                    
            case OpCode::Jump: {
                uint16_t offset = ReadShort();
                _frames[_frameCount - 1].ip += offset;
                break;
            }

            case OpCode::Loop: {
                uint16_t offset = ReadShort();
                _frames[_frameCount - 1].ip -= offset;
                break;
            }

            case OpCode::Not: {
                Push(Value(IsFalsey(Pop()))); 
                break;
            }

            case OpCode::GetLocal: {
                uint8_t slot = ReadByte();
                Push(_stack[CurrentFrame().slots_start + slot]);
                break;
            }

            case OpCode::SetLocal: {
                uint8_t slot = ReadByte();
                _stack[CurrentFrame().slots_start + slot] = Peek(0);
                break;
            }

            case OpCode::Pop: {
                Value discarded = Pop();
                if (CurrentFrame().function->Name == "repl_line" && discarded.type != ValueType::Nil) {
                    std::cout << discarded.ToString() << "\n";
                }
                break;
            }

            case OpCode::BuildMap: {
                uint8_t pairCount = ReadByte();
                auto map = std::make_shared<std::unordered_map<std::string, Value>>();

                for (int i = 0; i < pairCount; i++) {
                    Value value = Pop();
                    Value key = Pop();

                    if (key.type != ValueType::String) {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Type",
                            "Runtime Error: Expected string literal for dictionary property key, discovered '" + key.ToString() + "'.",
                            "",
                            "",
                            _frames[_frameCount - 1].ip,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });

                        return InterpretResult::RuntimeError;
                    }

                    (*map)[key.AsString()] = value;
                }

                Push(Value(map));
                break;
            }

            case OpCode::BuildArray: {
                uint8_t elementCount = ReadByte();
                auto array = std::make_shared<std::vector<Value>>();
                array->reserve(elementCount);

                for (int i = 0; i < elementCount; i++) {
                    array->push_back(Pop());
                }

                std::reverse(array->begin(), array->end());

                Push(Value(array));
                break;
            }

            case OpCode::GetIndex: {
                Value indexVal = Pop();
                Value collectionVal = Pop();

                if (collectionVal.type == ValueType::Dictionary) {
                    std::string key = indexVal.AsString();
                    auto& dict = collectionVal.AsDictionary();

                    auto it = dict.find(key);
                    if (it != dict.end()) {
                        Push(it->second);
                    }
                    else {
                        Push(Value()); // Push Nil
                    }
                }
                else if (collectionVal.type == ValueType::Array) {
                    int index = (int)indexVal.AsNumber();
                    std::vector<Value>& array = collectionVal.AsArray();

                    if (index < 0 || index >= (int)array.size()) {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Runtime",
                            "Runtime Error: Array collection index out of bounds boundaries.",
                            "",
                            "Check the array index is within valid range (0 - " + std::to_string(array.size() - 1) + ").",
                            (_frames[_frameCount - 1].function->chunk.Code.size() > 0) ? _frames[_frameCount - 1].ip : 1,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });
                        return InterpretResult::RuntimeError;
                    }
                    Push(array[index]);
                }
                else {
                    LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                        "Type Error",
                        "Runtime Error: Subscript indexing lookup operator is not supported on Type variant '" + collectionVal.ToString() + "'.",
                        "",
                        "",
                        _frames[_frameCount - 1].ip,
                        "main.a",
                        A::Core::Diagnostics::AErrorSeverity::Fatal
                    });
                    return InterpretResult::RuntimeError;
                }
                break;
            }

            case OpCode::SetIndex: {
                Value valueToSet = Pop();
                Value indexVal = Pop();
                Value collectionVal = Pop();

                if (collectionVal.type == ValueType::Dictionary) {
                    std::string key = indexVal.AsString();
                    auto& dict = collectionVal.AsDictionary();
                    
                    dict[key] = valueToSet;
                    Push(valueToSet);
                }
                else if (collectionVal.type == ValueType::Array) {
                    int index = (int)indexVal.AsNumber();
                    std::vector<Value>& array = collectionVal.AsArray();

                    if (index < 0 || index >= (int)array.size()) {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Runtime",
                            "Runtime Error: Array collection index out of bounds boundaries.",
                            "",
                            "Check the array index is within valid range (0 - " + std::to_string(array.size() - 1) + ").",
                            (_frames[_frameCount - 1].function->chunk.Code.size() > 0) ? _frames[_frameCount - 1].ip : 1,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });
                        return InterpretResult::RuntimeError;
                    }
                    array[index] = valueToSet;
                    Push(valueToSet);
                }
                else {
                    LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                        "Type Error",
                        "Runtime Error: Cannot mutate indexed index properties on Type variant '" + collectionVal.ToString() + "'.",
                        "",
                        "",
                        _frames[_frameCount - 1].ip,
                        "main.a",
                        A::Core::Diagnostics::AErrorSeverity::Fatal
                    });
                    return InterpretResult::RuntimeError;
                }
                break;
            }

            case OpCode::GetProperty: {
                uint8_t index = _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];
                std::string propertyName = _frames[activeFrameIndex].function->chunk.Constants[index].AsString();

                Value instanceValue = Pop(); // Extract the container/instance object from the value stack top

                // --- ROUTE 1: STATIC NAMESPACES & STATIC LIBRARIES ---
                if (instanceValue.type == ValueType::Module) {
                    auto& moduleDict = instanceValue.AsModule();
                    auto it = moduleDict.find(propertyName);
                    if (it != moduleDict.end()) {
                        Push(it->second);
                    }
                    else {
                        LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                            "Namespace Error",
                            "Runtime Error: Static component or method '" + propertyName + "' does not exist inside Module scope.",
                            "",
                            "",
                            _frames[activeFrameIndex].ip,
                            "main.a",
                            A::Core::Diagnostics::AErrorSeverity::Fatal
                        });
                        return InterpretResult::RuntimeError;
                    }
                    break;
                }

                // --- ROUTE 2: DYNAMIC USER DICTIONARIES ---
                if (instanceValue.type == ValueType::Dictionary) {
                    auto& dict = instanceValue.AsDictionary();
                    auto it = dict.find(propertyName);
                    if (it != dict.end()) {
                        Push(it->second);
                    }
                    else {
                        Push(Value()); // Fallback to Nil if missing safely
                    }
                    break;
                }

                // --- ROUTE 3: CLASS / STRUCT INSTANCES (DYNAMIC METHOD BINDING) ---
                if (instanceValue.type == ValueType::Instance) {
                    auto& instanceObj = instanceValue.AsInstance();

                    // A. State variables/Fields take explicit data priority tracking lookahead
                    auto fieldIt = instanceObj.fields.find(propertyName);
                    if (fieldIt != instanceObj.fields.end()) {
                        Push(fieldIt->second);
                        break;
                    }

                    // B. FIX: Look up the method dynamically using the qualified prefix (e.g., "Player.takeDamage")
                    std::string fullyQualifiedMethodName = instanceObj.className + "." + propertyName;
                    
                    auto symIt = _globalSymbolTable.find(fullyQualifiedMethodName);
                    if (symIt != _globalSymbolTable.end() && symIt->second < _globalCount) {
                        int methodSlot = symIt->second;
                        Value methodValue = _globalSlots[methodSlot];

                        Value boundMethodClosure = Value((NativeFunction)([instanceValue, methodValue](const std::vector<Value>& args) -> Value {
                            std::vector<Value> boundArgs;
                            boundArgs.reserve(args.size() + 1);
                            boundArgs.push_back(instanceValue); // Passes 'this' implicitly as parameter index 0
                            
                            for (const auto& arg : args) {
                                boundArgs.push_back(arg);
                            }
                            
                            if (methodValue.type == ValueType::Function) {
                                VM subExecutor;
                                subExecutor.Interpret(methodValue.function_val->chunk);
                                return Value();
                            }
                            else if (methodValue.type == ValueType::NativeFn) {
                                return methodValue.native_fn(boundArgs);
                            }
                            return Value();
                        }));
                        
                        Push(boundMethodClosure);
                        break;
                    }
                }

                // --- ROUTE 4: PRIMITIVE PROTOTYPES FALLBACK SUITE ---
                std::function<Value(const std::vector<Value>&)> resolvedPrimitiveMethod = nullptr;
                
                if (instanceValue.type == ValueType::String) {
                    auto protoIt = _stringPrototype.find(propertyName);
                    if (protoIt != _stringPrototype.end()) resolvedPrimitiveMethod = protoIt->second;
                }
                else if (instanceValue.type == ValueType::Array) {
                    auto protoIt = _arrayPrototype.find(propertyName);
                    if (protoIt != _arrayPrototype.end()) resolvedPrimitiveMethod = protoIt->second;
                }

                if (resolvedPrimitiveMethod != nullptr) {
                    Value primitiveBoundClosure = Value((NativeFunction)([instanceValue, resolvedPrimitiveMethod](const std::vector<Value>& args) -> Value {
                        std::vector<Value> boundArgs;
                        boundArgs.reserve(args.size() + 1);
                        boundArgs.push_back(instanceValue); // Passes primitive instance as first arg
                        
                        for (const auto& arg : args) {
                            boundArgs.push_back(arg);
                        }
                        return resolvedPrimitiveMethod(boundArgs);
                    }));
                    Push(primitiveBoundClosure);
                    break;
                }

                // COMPREHENSIVE TYPE SAFETY DEFENSE GUARD
                LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                    "Property Error",
                    "Runtime Error: Property or method '" + propertyName + "' does not exist on data Type variant '" + instanceValue.ToString() + "'.",
                    "",
                    "",
                    _frames[activeFrameIndex].ip,
                    "main.a",
                    A::Core::Diagnostics::AErrorSeverity::Fatal
                });
                return InterpretResult::RuntimeError;
            }

            case OpCode::SetProperty: {
                uint8_t index = _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];
                std::string propertyName = _frames[activeFrameIndex].function->chunk.Constants[index].AsString();

                // Value stack layout right now: [instance/container, value_to_assign] -> assigned value is on top
                Value valueToAssign = Pop();
                Value containerValue = Pop();

                // A. Handle mutations inside User Struct/Class instances dynamically
                if (containerValue.type == ValueType::Instance) {
                    auto& instanceObj = containerValue.AsInstance();
                    instanceObj.fields[propertyName] = valueToAssign; // Assign or update the field value
                    
                    // Push the assigned value back onto the stack to match expression semantics requirements
                    Push(valueToAssign); 
                }
                // B. Handle mutations inside standard Dictionaries dynamically
                else if (containerValue.type == ValueType::Dictionary) {
                    auto& dict = containerValue.AsDictionary();
                    dict[propertyName] = valueToAssign;
                    Push(valueToAssign);
                }
                else {
                    LastRuntimeError = std::make_shared<A::Core::Diagnostics::AError>(A::Core::Diagnostics::AError{
                        "Property Mutation Error",
                        "Runtime Error: Cannot assign property '" + propertyName + "' onto an immutable Type variant '" + containerValue.ToString() + "'.",
                        "",
                        "",
                        _frames[activeFrameIndex].ip,
                        "main.a",
                        A::Core::Diagnostics::AErrorSeverity::Fatal
                    });
                    return InterpretResult::RuntimeError;
                }
                break;
            }

            case OpCode::DefineClass: {
                // Read the class array slot operand byte directly, bypassing the constant pool
                uint8_t classSlotIndex = _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];

                // Initialize the class slot as a clean Module object container natively inside the global slots array
                auto emptyDict = std::make_shared<std::unordered_map<std::string, Value>>();
                _globalSlots[classSlotIndex] = Value(emptyDict);
                _globalSlots[classSlotIndex].type = ValueType::Module; // Force it to map as a module/class container namespace
                break;
            }

            case OpCode::Instantiate: {
                uint8_t classSlotIndex = _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];
                uint8_t argCount = _frames[activeFrameIndex].function->chunk.Code[_frames[activeFrameIndex].ip++];

                // Pop constructor parameters off the stack tracking allocations sequentially
                std::vector<Value> constructorArgs(argCount);
                for (int i = argCount - 1; i >= 0; i--) {
                    constructorArgs[i] = Pop();
                }

                // Discover the original class name string by searching the global symbol table mappings backward
                std::string className = "UserStructClass";
                for (const auto& kvp : _globalSymbolTable) {
                    if (kvp.second == classSlotIndex) {
                        className = kvp.first;
                        break;
                    }
                }
                
                // Build a fresh, isolated InstanceObject runtime heap container allocation
                std::unordered_map<std::string, Value> emptyMethodsVTable; 
                auto instanceObj = std::make_shared<InstanceObject>(className, emptyMethodsVTable);
                Value instanceValue(instanceObj);

                // AUTOMATED OPTIONAL CONSTRUCTOR INITIALIZATION PASS:
                std::string constructorName = className + ".init";
                auto symIt = _globalSymbolTable.find(constructorName);
                if (symIt != _globalSymbolTable.end()) {
                    int constructorSlot = symIt->second;
                    Value constructorMethod = _globalSlots[constructorSlot];
                    
                    if (constructorMethod.type == ValueType::Function) {
                        // Spawn the sub-executor engine context wrapper safely
                        VM subExecutor;

                        // Pre-seed your sub-executor's baseline value stack
                        // Push the newly created instance value straight into Stack Slot 0 ("this")
                        subExecutor.Push(instanceValue);

                        // Next, push the remainder of the constructor arguments right behind it
                        for (const auto& arg : constructorArgs) {
                            subExecutor.Push(arg);
                        }

                        // Execute the initialization chunk statements natively.
                        subExecutor.Interpret(constructorMethod.function_val->chunk);
                    }
                }

                // Push the completely populated structural instance object directly back onto the evaluation stack
                Push(instanceValue);
                break;
            }

            case OpCode::Return: {
                Value result = Pop(); // Grab the return value expression result
                _frameCount--; // Drop the completed execution frame

                if (_frameCount == 0) {
                    // If we finished executing the top-level script wrapper, terminate cleanly
                    return InterpretResult::Ok;
                }

                // Restore the stack top pointer position, popping local frames and parameters completely
                _stackTop = _frames[_frameCount].slots_start;
                Push(result);
                break;
            }

            default:
                return InterpretResult::RuntimeError;
        }
    }
    return InterpretResult::Ok;
}

void VM::RegisterNativeFunction(const std::string& name, int slotIndex, NativeFunction fn) {
    if (slotIndex >= 0 && slotIndex < 256) {
        _globalSlots[slotIndex] = Value(fn);
        _globalSymbolTable[name] = slotIndex;
        if (slotIndex >= _globalCount) {
            _globalCount = slotIndex + 1;
        }
    }
}