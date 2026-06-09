namespace Lexer
{
    public class NumberExpression : Expression
    {
        public double Value { get; }

        public NumberExpression(double value)
        {
            Value = value;
        }
    }

    public class StringExpression : Expression
    {
        public string Value { get; }

        public StringExpression(string value)
        {
            Value = value;
        }
    }

    public class BooleanExpression : Expression
    {
        public bool Value { get; }

        public BooleanExpression(bool value)
        {
            Value = value;
        }
    }

    public class VariableExpression : Expression
    {
        public string Name { get; }

        public VariableExpression(string name)
        {
            Name = name;
        }
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public TokenType Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(Expression left, TokenType @operator, Expression right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }
    }

    public class UnaryExpression : Expression
    {
        public TokenType Operator { get; }
        public Expression Right { get; }

        public UnaryExpression(TokenType @operator, Expression right)
        {
            Operator = @operator;
            Right = right;
        }
    }

    public class AssignExpression : Expression
    {
        public string Name { get; }
        public Expression Value { get; }

        public AssignExpression(string name, Expression value)
        {
            Name = name;
            Value = value;
        }
    }

    public class CallExpression : Expression
    {
        public string Callee { get; } 
        public List<Expression> Arguments { get; }

        public CallExpression(string callee, List<Expression> arguments)
        {
            Callee = callee;
            Arguments = arguments;
        }
    }

    public class ArrayExpression : Expression
    {
        public List<Expression> Elements { get; }

        public ArrayExpression(List<Expression> elements)
        {
            Elements = elements;
        }
    }

    public class IndexExpression : Expression
    {
        public Expression Array { get; }
        public Expression Index { get; }

        public IndexExpression(Expression array, Expression index)
        {
            Array = array;
            Index = index;
        }
    }

    public class IndexAssignExpression : Expression
    {
        public Expression Array { get; }
        public Expression Index { get; }
        public Expression Value { get; }

        public IndexAssignExpression(Expression array, Expression index, Expression value)
        {
            Array = array;
            Index = index;
            Value = value;
        }
    }
}
