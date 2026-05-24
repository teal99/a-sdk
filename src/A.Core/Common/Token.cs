namespace A.Core.Common;

public readonly struct Token
{
    public TokenType Type { get; }
    public string Lexeme { get; }
    public int Line { get; }
    public int Column { get; }

    public Token(TokenType type, string lexeme, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"[{Type, -12} | Lexeme: \"{Lexeme}\" | Line: {Line}, Column: {Column}]";
}