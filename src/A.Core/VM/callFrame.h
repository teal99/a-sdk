#ifndef CALLFRAME_H
#define CALLFRAME_H
#include <cstdint>
#include <memory>
#include <string>

struct CallFrame {
    std::shared_ptr<FunctionObject> function;
    int ip;
    int slots_start;

    CallFrame() {
        function = nullptr;
        ip = 0;
        slots_start = 0;
    }

    CallFrame(std::shared_ptr<FunctionObject> func, int slotsStart) {
        function = func;
        ip = 0;
        slots_start = slotsStart;
    }
};

#endif