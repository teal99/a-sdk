namespace A.Core.Parser;

public enum Precedence
{
    None,
    Assignment, // =
    Or, // or
    And, // and
    Comparison, // == < > =< >= !=
    Sum, // + -
    Product, // * / %
    Unary, // - !
    Call, // . ()
    Dot, // . (Binds namespaces together before execution calls)
    Primary
}