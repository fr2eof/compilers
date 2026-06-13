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

                case FunctionStatement function:
                    var savedDeclarations = Copy(declarations);
                    foreach (var param in function.Parameters)
                    {
                        declarations[param.Name] = (0, false);
                    }

                    VisitStatement(function.Body);

                    declarations.Clear();
                    foreach (var kv in savedDeclarations)
                    {
                        declarations[kv.Key] = kv.Value;
                    }
                    break;

                case ReturnStatement returnStmt:
                    if (returnStmt.Value is not null)
                    {
                        VisitExpression(returnStmt.Value);
                    }
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
                    
                case ArrayExpression arrayExpr:
                    foreach (var expr in arrayExpr.Elements) VisitExpression(expr);
                    break;
                case IndexExpression indexExpr:
                    VisitExpression(indexExpr.Array);
                    VisitExpression(indexExpr.Index);
                    break;
                case IndexAssignExpression idxAssign:
                    VisitExpression(idxAssign.Array);
                    VisitExpression(idxAssign.Index);
                    VisitExpression(idxAssign.Value);
                    break;

                case CallExpression call:
                    foreach (var arg in call.Arguments)
                    {
                        VisitExpression(arg);
                    }
                    break;

                    // NumberExpression / StringExpression / BooleanExpression — no variables to track
            }
        }

        private static Dictionary<string, (int Position, bool Used)> Copy(
            Dictionary<string, (int Position, bool Used)> source)
        {
            return new Dictionary<string, (int Position, bool Used)>(source);
        }
    }
}
