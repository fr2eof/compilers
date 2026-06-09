namespace Lexer
{
    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; }

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    public class PrintStatement : Statement
    {
        public Expression Expression { get; }

        public PrintStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    public class VarStatement : Statement
    {
        public string Name { get; }
        public TypeKind? DeclaredType { get; }
        public Expression? Initializer { get; }
        public int Position { get; }

        public VarStatement(string name, TypeKind? declaredType, Expression? initializer, int position = 0)
        {
            Name = name;
            DeclaredType = declaredType;
            Initializer = initializer;
            Position = position;
        }
    }

    public class BlockStatement : Statement
    {
        public List<Statement> Statements { get; }

        public BlockStatement(List<Statement> statements)
        {
            Statements = statements;
        }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public Statement ThenBranch { get; }
        public Statement? ElseBranch { get; }

        public IfStatement(Expression condition, Statement thenBranch, Statement? elseBranch = null)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(Expression condition, Statement body)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class Parameter
    {
        public string Name { get; }
        public TypeKind Type { get; }

        public Parameter(string name, TypeKind type)
        {
            Name = name;
            Type = type;
        }
    }

    public class FunctionStatement : Statement
    {
        public string Name { get; }
        public List<Parameter> Parameters { get; }
        public TypeKind ReturnType { get; }
        public Statement Body { get; }

        public FunctionStatement(string name, List<Parameter> parameters, TypeKind returnType, Statement body)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            Body = body;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression? Value { get; }

        public ReturnStatement(Expression? value)
        {
            Value = value;
        }
    }
}