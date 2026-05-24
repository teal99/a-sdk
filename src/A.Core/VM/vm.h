#ifndef VM_H
#define VM_H
#include <vector>
#include <cstdint>
#include <unordered_map>
#include <string>
#include "../Common/value.h"
#include "callFrame.h"
#include "../Compiler/structures.h"
#include "../Compiler/compiler.h"
#include "../Diagnostics/diagnostics.h"
#include "opcode.h"

class VM {
public:
    enum class InterpretResult {
        Ok,
        CompileError,
        RuntimeError
    };

    std::shared_ptr<A::Core::Diagnostics::AError> LastRuntimeError = nullptr;

    VM();
    ~VM() = default;
    InterpretResult Interpret(Chunk chunk, bool isReplMode = false);
    void RegisterNativeFunction(const std::string& name, int slotIndex, NativeFunction fn);

private:
    static const std::unordered_map<std::string, std::function<Value(const std::vector<Value>&)>> _stringPrototype;
    static const std::unordered_map<std::string, std::function<Value(const std::vector<Value>&)>> _arrayPrototype;

    std::array<CallFrame, 1024> _frames{};
    int _frameCount = 0;
    
    std::array<Value, 2048> _stack{};
    int _stackTop = 0;

    std::array<Value, 256> _globalSlots{};
    std::unordered_map<std::string, int> _globalSymbolTable;
    int _globalCount = 0;

    // Core Execution Pipeline
    InterpretResult Run();

    // --- VM STACK UTILITIES ---
    void Push(Value value);
    Value Pop();
    Value Peek(int distance);
    void BinaryOp(std::function<Value(double, double)> operation);
    void CompareOp(std::function<bool(double, double)> operation);

    // --- BYTE NAVIGATION UTILITIES ---
    CallFrame& CurrentFrame();
    uint8_t ReadByte();
    Value ReadConstant();
    uint16_t ReadShort();

    // --- EXTRA HELPERS ---
    static bool IsFalsey(Value value);
};

#endif