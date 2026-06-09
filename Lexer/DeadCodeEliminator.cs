namespace Lexer
{
    /// <summary>
    /// Шаг 3: Оптимизация AST (Dead Code Elimination / DCE)
    /// Сканирует дерево на наличие "мертвого кода", который никогда не выполнится 
    /// (например `if (false)` или код после оператора `return`) и безвозвратно удаляет его.
    /// Это делает итоговый скомпилированный код меньше и быстрее.
    /// </summary>
    public class DeadCodeEliminator
    {
        public List<Statement> Optimize(IEnumerable<Statement> statements)
        {
            var result = new List<Statement>();

            foreach (var stmt in statements)
            {
                var optimized = OptimizeStatement(stmt);
                if (optimized != null)
                {
                    result.Add(optimized);

                    // Код после return - мертвый. Выходим из блока.
                    if (IsAlwaysReturning(optimized))
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private Statement? OptimizeStatement(Statement statement)
        {
            switch (statement)
            {
                case BlockStatement block:
                    var optimizedBlock = Optimize(block.Statements);
                    if (optimizedBlock.Count == 0) return null; // Or empty block
                    return new BlockStatement(optimizedBlock);

                case IfStatement ifStmt:
                    if (ifStmt.Condition is BooleanExpression boolExp)
                    {
                        if (boolExp.Value)
                        {
                            return OptimizeStatement(ifStmt.ThenBranch);
                        }
                        else if (ifStmt.ElseBranch != null)
                        {
                            return OptimizeStatement(ifStmt.ElseBranch);
                        }
                        else
                        {
                            return null; // Конструкция целиком мертвая
                        }
                    }

                    var thenBr = OptimizeStatement(ifStmt.ThenBranch);
                    var elseBr = ifStmt.ElseBranch != null ? OptimizeStatement(ifStmt.ElseBranch) : null;
                    if (thenBr == null && elseBr == null)
                    {
                        thenBr = new BlockStatement(new List<Statement>());
                    }
                    return new IfStatement(ifStmt.Condition, thenBr ?? new BlockStatement(new List<Statement>()), elseBr);

                case WhileStatement whileStmt:
                    if (whileStmt.Condition is BooleanExpression wBoolExp && !wBoolExp.Value)
                    {
                        return null; // Мертвый цикл, условие всегда ложно!
                    }

                    var body = OptimizeStatement(whileStmt.Body);
                    return new WhileStatement(whileStmt.Condition, body ?? new BlockStatement(new List<Statement>()));

                case FunctionStatement func:
                    var funcBody = OptimizeStatement(func.Body);
                    return new FunctionStatement(func.Name, func.Parameters, func.ReturnType, funcBody ?? new BlockStatement(new List<Statement>()));

                default:
                    // ReturnStatement, ExpressionStatement, VarStatement, PrintStatement
                    return statement;
            }
        }

        private bool IsAlwaysReturning(Statement statement)
        {
            if (statement is ReturnStatement) return true;
            if (statement is BlockStatement block && block.Statements.Count > 0)
            {
                return IsAlwaysReturning(block.Statements.Last());
            }
            if (statement is IfStatement ifStmt)
            {
                return ifStmt.ElseBranch != null && IsAlwaysReturning(ifStmt.ThenBranch) && IsAlwaysReturning(ifStmt.ElseBranch);
            }
            return false;
        }
    }
}
