namespace Lexer
{
    public class UninitializedVariableChecker
    {
        private readonly List<string> errors = new();

        // name -> (declaration position, is definitely initialized)
        private Dictionary<string, (int Position, bool Initialized)> env = new();

        public IReadOnlyList<string> Errors => errors;

        public void Check(IEnumerable<Statement> statements)
        {
            foreach (var statement in statements)
            {
                VisitStatement(statement);
            }
        }

        private void VisitStatement(Statement statement)
        {
            switch (statement)
            {
                case VarStatement var:
                    if (var.Initializer is not null)
                    {
                        VisitExpression(var.Initializer);
                        env[var.Name] = (var.Position, true);
                    }
                    else
                    {
                        env[var.Name] = (var.Position, false);
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

                    // Snapshot environment before branching
                    var beforeIf = Copy(env);

                    VisitStatement(conditional.ThenBranch);
                    var afterThen = Copy(env);

                    // Restore snapshot for else branch
                    env = Copy(beforeIf);

                    if (conditional.ElseBranch is not null)
                    {
                        VisitStatement(conditional.ElseBranch);
                        // Variable is definitely initialized only if BOTH branches initialize it
                        env = Intersect(afterThen, env);
                    }
                    else
                    {
                        // No else: then-branch initialization is not guaranteed
                        env = Intersect(afterThen, beforeIf);
                    }
                    break;

                case WhileStatement loop:
                    var beforeWhile = Copy(env);

                    // Condition is evaluated before the body
                    VisitExpression(loop.Condition);
                    VisitStatement(loop.Body);

                    // Body may not execute at all — intersect to keep only definitely-initialized vars
                    env = Intersect(env, beforeWhile);
                    break;
            }
        }

        private void VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case VariableExpression variable:
                    if (env.TryGetValue(variable.Name, out var info) && !info.Initialized)
                    {
                        errors.Add(
                            $"[Error] Position {info.Position}: Variable '{variable.Name}' is used before being initialized.");
                    }
                    break;

                case AssignExpression assign:
                    // Evaluate RHS first, then mark the variable as initialized
                    VisitExpression(assign.Value);
                    if (env.TryGetValue(assign.Name, out var declInfo))
                    {
                        env[assign.Name] = (declInfo.Position, true);
                    }
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
            }
        }

        private static Dictionary<string, (int Position, bool Initialized)> Copy(
            Dictionary<string, (int Position, bool Initialized)> source)
        {
            return new Dictionary<string, (int Position, bool Initialized)>(source);
        }

        // A variable is definitely initialized after a merge only if it was initialized in BOTH branches.
        private static Dictionary<string, (int Position, bool Initialized)> Intersect(
            Dictionary<string, (int Position, bool Initialized)> a,
            Dictionary<string, (int Position, bool Initialized)> b)
        {
            var result = new Dictionary<string, (int Position, bool Initialized)>();

            foreach (var key in a.Keys.Union(b.Keys))
            {
                var inA = a.TryGetValue(key, out var av);
                var inB = b.TryGetValue(key, out var bv);

                if (inA && inB)
                {
                    result[key] = (av.Position, av.Initialized && bv.Initialized);
                }
                else if (inA)
                {
                    result[key] = av;
                }
                else
                {
                    result[key] = bv;
                }
            }

            return result;
        }
    }
}
