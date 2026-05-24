#include "diagnostics.h"
#include "../VM/opcode.h"
#include <iostream>
#include <iomanip>

namespace A::Core::Diagnostics {

    void DiagnosticPrinter::Print(const AError& error) {
        switch (error.Severity) {
            case AErrorSeverity::Fatal:   std::cout << "\033[31m"; break;
            case AErrorSeverity::Error:   std::cout << "\033[91m"; break;
            case AErrorSeverity::Warning: std::cout << "\033[93m"; break;
            default:                      std::cout << "\033[96m"; break;
        }

        std::string origin = error.File.empty() ? "Line " + std::to_string(error.Line) : error.File + ":" + std::to_string(error.Line);
        
        std::string severityStr;
        switch (error.Severity) {
            case AErrorSeverity::Info:    severityStr = "INFO"; break;
            case AErrorSeverity::Warning: severityStr = "WARNING"; break;
            case AErrorSeverity::Error:   severityStr = "ERROR"; break;
            case AErrorSeverity::Fatal:   severityStr = "FATAL"; break;
        }

        std::cout << "\n[" << severityStr << " - " << error.Type << " Error] (" << origin << ")\033[0m\n";
        std::cout << "  " << error.Message << "\n";

        if (!error.ExtraInfo.empty()) std::cout << "\033[90m  Context: " << error.ExtraInfo << "\033[0m\n";
        if (!error.Suggestion.empty()) std::cout << "\033[92m  Suggestion: " << error.Suggestion << "\033[0m\n";
        std::cout << std::endl;
    }

    std::string Disassembler::OpCodeToString(OpCode code) {
        switch (code) {
            case OpCode::Constant:         return "CONSTANT";
            case OpCode::Add:              return "ADD";
            case OpCode::Subtract:          return "SUBTRACT";
            case OpCode::Multiply:          return "MULTIPLY";
            case OpCode::Divide:            return "DIVIDE";
            case OpCode::Modulo:            return "MODULO";
            case OpCode::Negate:            return "NEGATE";
            case OpCode::DefineGlobal:     return "DEFINE_GLOBAL";
            case OpCode::GetGlobal:        return "GET_GLOBAL";
            case OpCode::AssignGlobal:     return "ASSIGN_GLOBAL";
            case OpCode::GetGlobalFast:    return "GET_GLOBAL_FAST";
            case OpCode::AssignGlobalFast: return "ASSIGN_GLOBAL_FAST";
            case OpCode::GetLocal:         return "GET_LOCAL";
            case OpCode::SetLocal:         return "SET_LOCAL";
            case OpCode::BuildArray:       return "BUILD_ARRAY";
            case OpCode::GetIndex:         return "GET_INDEX";
            case OpCode::SetIndex:         return "SET_INDEX";
            case OpCode::BuildMap:         return "BUILD_MAP";
            case OpCode::GetProperty:      return "GET_PROPERTY";
            case OpCode::SetProperty:      return "SET_PROPERTY";
            case OpCode::DefineClass:      return "DEFINE_CLASS";
            case OpCode::Instantiate:      return "INSTANTIATE";
            case OpCode::Pop:              return "POP";
            case OpCode::True:             return "TRUE";
            case OpCode::False:            return "FALSE";
            case OpCode::Equal:            return "EQUAL";
            case OpCode::Greater:          return "GREATER";
            case OpCode::Lesser:           return "LESSER";
            case OpCode::GreaterEqual:     return "GREATER_EQUAL";
            case OpCode::LesserEqual:      return "LESSER_EQUAL";
            case OpCode::EqualEqual:       return "EQUAL_EQUAL";
            case OpCode::UnEqual:          return "UNEQUAL";
            case OpCode::Not:              return "NOT";
            case OpCode::Call:             return "CALL";
            case OpCode::JumpIfFalse:      return "JUMP_IF_FALSE";
            case OpCode::Jump:             return "JUMP";
            case OpCode::Loop:             return "LOOP";
            case OpCode::Nil:              return "NIL";
            case OpCode::Return:           return "RETURN";
            default:                       return "UNKNOWN_OP";
        }
    }

    int Disassembler::SimpleInstruction(const std::string& name, int offset) {
        std::cout << name << "\n";
        return offset + 1;
    }

    int Disassembler::ByteInstruction(const std::string& name, const Chunk& chunk, int offset) {
        if (offset + 1 >= (int)chunk.Code.size()) return offset + 1;
        uint8_t slot = chunk.Code[offset + 1];
        std::cout << std::left << std::setw(16) << name << " " << std::setw(3) << std::setfill('0') << (int)slot << "\n";
        return offset + 2;
    }

    int Disassembler::ConstantInstruction(const std::string& name, const Chunk& chunk, int offset) {
        if (offset + 1 >= (int)chunk.Code.size()) return offset + 1;
        uint8_t constantIndex = chunk.Code[offset + 1];
        std::cout << std::left << std::setw(16) << name << " " << std::setw(3) << std::setfill('0') << (int)constantIndex << " ";
        if (constantIndex < chunk.Constants.size()) {
            std::cout << "(" << chunk.Constants[constantIndex].ToString() << ")\n";
        } else {
            std::cout << "(Direct Index Operand)\n";
        }
        return offset + 2;
    }

    int Disassembler::JumpInstruction(const std::string& name, int sign, const Chunk& chunk, int offset) {
        if (offset + 2 >= (int)chunk.Code.size()) return offset + 1;
        uint16_t jump = static_cast<uint16_t>(chunk.Code[offset + 1] << 8);
        jump |= chunk.Code[offset + 2];
        std::cout << std::left << std::setw(16) << name << " " << offset << " -> " << (offset + 3 + sign * jump) << "\n";
        return offset + 3;
    }

    int Disassembler::DisassembleInstruction(const Chunk& chunk, int offset) {
        std::cout << std::setw(4) << std::setfill('0') << offset << "  ";

        uint8_t instruction = chunk.Code[offset];
        OpCode code = static_cast<OpCode>(instruction);

        switch (code) {
            case OpCode::Return:
            case OpCode::Pop:
            case OpCode::Add:
            case OpCode::Subtract:
            case OpCode::Multiply:
            case OpCode::Divide:
            case OpCode::Negate:
            case OpCode::Lesser:
            case OpCode::Greater:
            case OpCode::LesserEqual:
            case OpCode::GreaterEqual:
            case OpCode::EqualEqual:
            case OpCode::UnEqual:
            case OpCode::Not:
            case OpCode::True:
            case OpCode::False:
            case OpCode::Nil:
            case OpCode::GetIndex:
            case OpCode::SetIndex:
                return SimpleInstruction(OpCodeToString(code), offset);
            case OpCode::Constant:
            case OpCode::DefineGlobal:
            case OpCode::GetGlobal:
            case OpCode::AssignGlobal:
            case OpCode::GetLocal:
            case OpCode::SetLocal:
            case OpCode::GetProperty:
            case OpCode::SetProperty:
                return ConstantInstruction(OpCodeToString(code), chunk, offset);
            case OpCode::GetGlobalFast:
            case OpCode::AssignGlobalFast:
            case OpCode::Call:
            case OpCode::BuildMap:
            case OpCode::BuildArray:
                return ByteInstruction(OpCodeToString(code), chunk, offset);
            case OpCode::JumpIfFalse:
            case OpCode::Jump:
                return JumpInstruction(OpCodeToString(code), 1, chunk, offset);
            case OpCode::Loop:
                return JumpInstruction("OP_LOOP", -1, chunk, offset);
            default:
                std::cout << "Unknown opcode format " << (int)instruction << "\n";
                throw std::runtime_error("Invalid opcode exception encountered.");
        }
    }

    void Disassembler::Disassemble(const Chunk& chunk, const std::string& name) {
        std::cout << "=== " << name << " ===\n";
        int offset = 0;
        while (offset < (int)chunk.Code.size()) {
            offset = DisassembleInstruction(chunk, offset);
        }
        std::cout << "===============\n";
    }
}