#ifndef DIAGNOSTICS_H
#define DIAGNOSTICS_H
#include <string>
#include <vector>
#include "../Compiler/structures.h"
#include "../VM/opcode.h"

namespace A::Core::Diagnostics {

    enum class AErrorSeverity {
        Info,
        Warning,
        Error,
        Fatal
    };

    struct AError {
        std::string Type;
        std::string Message;
        std::string ExtraInfo;
        std::string Suggestion;
        int Line;
        std::string File;
        AErrorSeverity Severity;
    };

    class DiagnosticPrinter {
    public:
        static void Print(const AError& error);
    };

    class Disassembler {
    public:
        static void Disassemble(const Chunk& chunk, const std::string& name);
        static int DisassembleInstruction(const Chunk& chunk, int offset);

    private:
        static int SimpleInstruction(const std::string& name, int offset);
        static int ByteInstruction(const std::string& name, const Chunk& chunk, int offset);
        static int ConstantInstruction(const std::string& name, const Chunk& chunk, int offset);
        static int JumpInstruction(const std::string& name, int sign, const Chunk& chunk, int offset);
        static std::string OpCodeToString(OpCode code);
    };
}

#endif