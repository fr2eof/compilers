namespace Lexer
{
    public class UnusedVariableChecker
    {
        // name -> (declaration position, was ever read)
        private readonly Dictionary<string, (int Position, bool Used)> declarations = new();
        private readonly List<string> warnings = new();

        public IReadOnlyList<string> Warnings => warnings;

        public void Check(IEnumerable<Statement> statements)
        {
            foreach (var statement in statements)
            {
                VisitStatement(statement);
            }

            foreach (var (name, (position, used)) in declarations)
            {
                if (!used)
                {
                    warnings.Add($"[Warning] Position {position}: Variable '{name}' is declared but never used.");
                }
            }
        }

        private void VisitStatement(Statement statement)
        {
            switch (statement)
            {
                case VarStatement var:
                    // Register the declaration first, then visit initializer (reads count as uses)
                    if (!declarations.ContainsKey(var.Name))
                    {
                        declarations[var.Name] = (var.Position, false);
                    }
                    if (var.Initializer is not null)
                    {
                        VisitExpression(var.Initializer);
                    }
                    break;

                case PrintStatement print:
                    VisitExpression(print.Expression);
                    break;

                case ExpressionStatement expr:
                    VisitExpression(expr.Expression);
                    break;

                case BlockStatement block:
                    foreach (var s in block.Statements)
                    {
                        VisitStatement(s);
                    }
                    break;

                case IfStatement conditional:
                    VisitExpression(conditional.Condition);
                    VisitStatement(conditional.ThenBranch);
                    if (conditional.ElseBranch is not null)
                    {
                        VisitStatement(conditional.ElseBranch);
                    }
                    break;

                case WhileStatement loop:
                    VisitExpression(loop.Condition);
                    VisitStatement(loop.Body);
                    break;
            }
        }

        private void VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case VariableExpression variable:
                    if (declarations.ContainsKey(variable.Name))
                    {
                        declarations[variable.Name] = (declarations[variable.Name].Position, true);
                    }
                    break;

                case AssignExpression assign:
                    // The right-hand side is a read; the variable being assigned to is NOT a read
                    VisitExpression(assign.Value);
                    break;

                case BinaryExpression binary:
                    VisitExpression(binary.Left);
                    VisitExpression(binary.Right);
                    break;

                case UnaryExpression unary:
                    VisitExpression(unary.Right);
                    break;

                case CallExpression call:
                    foreach (var arg in call.Arguments)
                    {
                        VisitExpression(arg);
                    }
                    break;

                    // NumberExpression / StringExpression - no variables to track
            }
        }
    }
}
