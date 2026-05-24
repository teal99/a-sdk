namespace A.Core.Common;

public enum TokenType
{
    // Single-character tokens
    LeftParen, RightParen, LeftBrace, RightBrace, LeftBracket, RightBracket, Colon,
    Plus, Minus, Star, Slash, Modulo,
    Equal, Less, Greater,
    Dot, Comma, Bang,
    // Multi-character operators
    EqualEqual, UnEqual, LessEqual, GreaterEqual, Arrow, // Arrow is =>
    // Literals
    Identifier, Number, String,
    // Keywords
    Let, Mut, 
    Fn, If, Else, While, Return, 
    True, False, And, Or,
    Use, As,
    New, Class, Struct,
    // Structural / Whitespace control
    Newline,
    EOF,
    Error
}