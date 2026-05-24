#ifndef OPCODE_H
#define OPCODE_H
#include <cstdint>

enum class OpCode : uint8_t {
    Constant,         // 0
    Add,              // 1
    Subtract,         // 2
    Multiply,         // 3
    Divide,           // 4
    Modulo,           // 5
    Negate,           // 6
    DefineGlobal,     // 7
    GetGlobal,        // 8
    AssignGlobal,     // 9
    GetGlobalFast,    // 10
    AssignGlobalFast, // 11
    GetLocal,         // 12
    SetLocal,         // 13
    BuildArray,       // 14
    GetIndex,         // 15
    SetIndex,         // 16
    BuildMap,         // 17
    GetProperty,      // 18
    SetProperty,      // 19
    DefineClass,      // 20
    Instantiate,      // 21
    Pop,              // 22
    True,             // 23
    False,            // 24
    Equal,            // 25
    Greater,          // 26
    Lesser,           // 27
    GreaterEqual,     // 28
    LesserEqual,      // 29
    EqualEqual,       // 30
    UnEqual,          // 31
    Not,              // 32
    Call,             // 33
    JumpIfFalse,      // 34
    Jump,             // 35
    Loop,             // 36
    Nil,              // 37
    Return            // 38
};

#endif