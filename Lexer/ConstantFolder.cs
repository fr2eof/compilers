namespace Lexer
{
    /// <summary>
    /// Платформенно-независимая оптимизация AST.
    /// Сворачивает выражения с литералами на этапе компиляции.
    /// </summary>
    public sealed class ConstantFolder
    {
        public List<Statement> Optimize(IEnumerable<Statement> statements)
        {
            var optimized = new List<Statement>();

            foreach (Statement statement in statements)
            {
                optimized.Add(OptimizeStatement(statement));
            }

            return optimized;
        }

        private Statement OptimizeStatement(Statement statement)
        {
            switch (statement)
            {
                case BlockStatement block:
                    return new BlockStatement(Optimize(block.Statements));

                case IfStatement ifStatement:
                    return new IfStatement(
                        OptimizeExpression(ifStatement.Condition),
                        OptimizeStatement(ifStatement.ThenBranch),
                        ifStatement.ElseBranch is not null ? OptimizeStatement(ifStatement.ElseBranch) : null);

                case WhileStatement whileStatement:
                    return new WhileStatement(
                        OptimizeExpression(whileStatement.Condition),
                        OptimizeStatement(whileStatement.Body));

                case FunctionStatement functionStatement:
                    return new FunctionStatement(
                        functionStatement.Name,
                        functionStatement.Parameters,
                        functionStatement.ReturnType,
                        OptimizeStatement(functionStatement.Body));

                case ReturnStatement returnStatement:
                    return new ReturnStatement(
                        returnStatement.Value is not null ? OptimizeExpression(returnStatement.Value) : null);

                case PrintStatement printStatement:
                    return new PrintStatement(OptimizeExpression(printStatement.Expression));

                case ExpressionStatement expressionStatement:
                    return new ExpressionStatement(OptimizeExpression(expressionStatement.Expression));

                case VarStatement varStatement:
                    return new VarStatement(
                        varStatement.Name,
                        varStatement.DeclaredType,
                        varStatement.Initializer is not null ? OptimizeExpression(varStatement.Initializer) : null,
                        varStatement.Position);

                default:
                    return statement;
            }
        }

        private Expression OptimizeExpression(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    return OptimizeBinaryExpression(binary);

                case UnaryExpression unary:
                    return OptimizeUnaryExpression(unary);

                case AssignExpression assign:
                    return new AssignExpression(assign.Name, OptimizeExpression(assign.Value));

                case CallExpression call:
                    return new CallExpression(
                        call.Callee,
                        call.Arguments.Select(OptimizeExpression).ToList());

                default:
                    return expression;
            }
        }

        private Expression OptimizeBinaryExpression(BinaryExpression binary)
        {
            Expression left = OptimizeExpression(binary.Left);
            Expression right = OptimizeExpression(binary.Right);

            if (left is NumberExpression leftNumber && right is NumberExpression rightNumber)
            {
                return FoldNumberBinary(binary.Operator, leftNumber.Value, rightNumber.Value);
            }

            if (left is StringExpression leftString && right is StringExpression rightString &&
                binary.Operator == TokenType.PLUS)
            {
                return new StringExpression(leftString.Value + rightString.Value);
            }

            return new BinaryExpression(left, binary.Operator, right);
        }

        private static Expression FoldNumberBinary(TokenType op, double left, double right)
        {
            return op switch
            {
                TokenType.PLUS => new NumberExpression(left + right),
                TokenType.MINUS => new NumberExpression(left - right),
                TokenType.STAR => new NumberExpression(left * right),
                TokenType.SLASH => new NumberExpression(left / right),
                _ => new BinaryExpression(new NumberExpression(left), op, new NumberExpression(right)),
            };
        }

        private Expression OptimizeUnaryExpression(UnaryExpression unary)
        {
            Expression right = OptimizeExpression(unary.Right);

            if (right is NumberExpression number && unary.Operator == TokenType.MINUS)
            {
                return new NumberExpression(-number.Value);
            }

            if (right is BooleanExpression boolean && unary.Operator == TokenType.EXCL)
            {
                return new BooleanExpression(!boolean.Value);
            }

            return new UnaryExpression(unary.Operator, right);
        }
    }
}
