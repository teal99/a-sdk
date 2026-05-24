using System;
using A.Core.Compiler;
using A.Core.VM;

namespace A.Core.Diagnostics;

public static class Disassembler
{
    public static void Disassemble(Chunk chunk, string name)
    {
        Console.WriteLine($"=== {name} ===");
        int offset = 0;
        while (offset < chunk.Code.Count)
        {
            offset = DisassembleInstruction(chunk, offset);
        }
        Console.WriteLine($"===============");
    }

    public static int DisassembleInstruction(Chunk chunk, int offset)
    {
        Console.Write($"{offset:D4}  ");
        byte instruction = chunk.Code[offset];
        if (!Enum.IsDefined(typeof(OpCode), instruction))
        {
            Console.WriteLine($"Unknown opcode {instruction}");
            return offset + 1;
        }

        OpCode code = (OpCode)chunk.Code[offset];
        switch (code)
        {
            case OpCode.Return:
            case OpCode.Pop:
            case OpCode.Add:
            case OpCode.Subtract:
            case OpCode.Multiply:
            case OpCode.Divide:
            case OpCode.Negate:
            case OpCode.Lesser:
            case OpCode.Greater:
            case OpCode.LesserEqual:
            case OpCode.GreaterEqual:
            case OpCode.EqualEqual:
            case OpCode.UnEqual:
            case OpCode.Not:
            case OpCode.True:
            case OpCode.False:
            case OpCode.Nil:
            case OpCode.GetIndex:
            case OpCode.SetIndex:
                return SimpleInstruction(code.ToString().ToUpper(), offset);
            case OpCode.Constant:
            case OpCode.DefineGlobal:
            case OpCode.GetGlobal:
            case OpCode.AssignGlobal:
            case OpCode.GetLocal:
            case OpCode.SetLocal:
            case OpCode.GetProperty:
            case OpCode.SetProperty:
                return ConstantInstruction(code.ToString().ToUpper(), chunk, offset);
            case OpCode.GetGlobalFast:
            case OpCode.AssignGlobalFast:
            case OpCode.Call:
            case OpCode.BuildMap:
            case OpCode.BuildArray:
                return ByteInstruction(code.ToString().ToUpper(), chunk, offset);
            case OpCode.JumpIfFalse:
            case OpCode.Jump:
                return JumpInstruction(code.ToString().ToUpper(), 1, chunk, offset);
            case OpCode.Loop:
                return JumpInstruction("OP_LOOP", -1, chunk, offset);
            default:
                Console.WriteLine($"Unknown opcode format {code}");
                throw new Exception($"Invalid opcode at {offset}: {instruction}");
        }
    }

    private static int SimpleInstruction(string name, int offset)
    {
        Console.WriteLine(name);
        return offset + 1;
    }

    private static int ByteInstruction(string name, Chunk chunk, int offset)
    {
        if (offset + 1 >= chunk.Code.Count) return offset + 1;
        byte slot = chunk.Code[offset + 1];
        Console.WriteLine($"{name,-16} {slot:D3}");
        return offset + 2;
    }

    private static int ConstantInstruction(string name, Chunk chunk, int offset)
    {
        if (offset + 1 >= chunk.Code.Count) return offset + 1;
        byte constantIndex = chunk.Code[offset + 1];
        Console.Write($"{name,-16} {constantIndex:D3} ");
        if (constantIndex < chunk.Constants.Count)
            Console.WriteLine($"({chunk.Constants[constantIndex]})");
        else
            Console.WriteLine("(Direct Index Operand)");
        return offset + 2;
    }

    private static int JumpInstruction(string name, int sign, Chunk chunk, int offset)
    {
        if (offset + 2 >= chunk.Code.Count) return offset + 1;
        ushort jump = (ushort)(chunk.Code[offset + 1] << 8);
        jump |= chunk.Code[offset + 2];
        Console.WriteLine($"{name,-16} {offset} -> {offset + 3 + sign * jump}");
        return offset + 3;
    }
}