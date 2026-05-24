#include "vm.h"
#include <vector>
#include <cstdint>

typedef void (*StdLibCallback)(int functionSlotIndex);

extern "C" {
    __declspec(dllexport) int ExecuteNativeBytecode(uint8_t* code, int codeLength, StdLibCallback callback) {
        Chunk chunk;
        chunk.Code = std::vector<uint8_t>(code, code + codeLength);

        VM nativeMachine;

        VM::InterpretResult result = nativeMachine.Interpret(chunk);

        return (result == VM::InterpretResult::Ok) ? 0 : 1;
    }
}