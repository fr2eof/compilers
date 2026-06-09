namespace Lexer
{
    /// <summary>
    /// Шаг 3: Семантический анализ (Проверка типов).
    /// Этот модуль проверяет код на соответствие типам до его запуска. 
    /// Он гарантирует, что мы не сложим Number и String напрямую 
    /// и что мы вызываем функции с правильными типами аргументов.
    /// </summary>
    public class TypeChecker
    {
        private readonly List<string> errors = new();
        private readonly Dictionary<string, TypeKind> variableTypes = new();
        private readonly Dictionary<string, FunctionStatement> functions = new();

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
                case FunctionStatement function:
                    functions[function.Name] = function;

                    var beforeFunc = Copy(variableTypes);
                    foreach (var param in function.Parameters)
                    {
                        variableTypes[param.Name] = param.Type;
                    }
                    VisitStatement(function.Body);

                    variableTypes.Clear();
                    foreach (var kv in beforeFunc)
                    {
                        variableTypes[kv.Key] = kv.Value;
                    }
                    break;

                case ReturnStatement returnStmt:
                    if (returnStmt.Value is not null)
                    {
                        VisitExpression(returnStmt.Value);
                    }
                    break;

                case VarStatement var:
                    RegisterVariable(var);
                    break;

                case PrintStatement print:
                    VisitExpression(print.Expression);
                    break;

                case ExpressionStatement expressionStatement:
                    VisitExpression(expressionStatement.Expression);
                    break;

                case BlockStatement block:
                    foreach (var nestedStatement in block.Statements)
                    {
                        VisitStatement(nestedStatement);
                    }
                    break;

                case IfStatement conditional:
                    TypeKind conditionType = VisitExpression(conditional.Condition);
                    EnsureBooleanCondition(conditionType, "if");

                    var beforeIf = Copy(variableTypes);

                    VisitStatement(conditional.ThenBranch);
                    var afterThen = Copy(variableTypes);

                    variableTypes.Clear();
                    foreach (var kv in beforeIf)
                    {
                        variableTypes[kv.Key] = kv.Value;
                    }

                    if (conditional.ElseBranch is not null)
                    {
                        VisitStatement(conditional.ElseBranch);
                        var afterElse = Copy(variableTypes);
                        MergeBranchTypes(afterThen, afterElse);
                    }
                    else
                    {
                        MergeBranchTypes(afterThen, beforeIf);
                    }
                    break;

                case WhileStatement loop:
                    TypeKind whileConditionType = VisitExpression(loop.Condition);
                    EnsureBooleanCondition(whileConditionType, "while");

                    var beforeLoop = Copy(variableTypes);
                    VisitStatement(loop.Body);
                    var afterLoopBody = Copy(variableTypes);
                    MergeBranchTypes(afterLoopBody, beforeLoop);
                    break;
            }
        }

        private TypeKind VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression:
                    return TypeKind.Number;

                case StringExpression:
                    return TypeKind.String;

                case BooleanExpression:
                    return TypeKind.Bool;

                case VariableExpression variable:
                    if (!variableTypes.TryGetValue(variable.Name, out var variableType))
                    {
                        errors.Add($"[Type Error] Variable '{variable.Name}' is not declared.");
                        return TypeKind.Unknown;
                    }

                    if (variableType == TypeKind.Unknown)
                    {
                        errors.Add($"[Type Error] Variable '{variable.Name}' has unknown type.");
                    }

                    return variableType;

                case AssignExpression assign:
                    TypeKind valueType = VisitExpression(assign.Value);

                    if (!variableTypes.TryGetValue(assign.Name, out var currentType))
                    {
                        errors.Add($"[Type Error] Variable '{assign.Name}' is not declared.");
                        return TypeKind.Unknown;
                    }

                    if (currentType == TypeKind.Unknown)
                    {
                        errors.Add($"[Type Error] Variable '{assign.Name}' has unknown type and cannot be assigned strictly.");
                        return TypeKind.Unknown;
                    }

                    if (valueType == TypeKind.Unknown)
                    {
                        return currentType;
                    }

                    if (currentType != valueType)
                    {
                        errors.Add($"[Type Error] Cannot assign value of type {TypeName(valueType)} to variable '{assign.Name}' of type {TypeName(currentType)}.");
                    }

                    return currentType;

                case BinaryExpression binary:
                    return CheckBinary(binary);

                default:
                    return TypeKind.Unknown;
            }
        }

        private TypeKind CheckCall(CallExpression call)
        {
            if (!functions.TryGetValue(call.Callee, out var function))
            {
                errors.Add($"[Type Error] Function '{call.Callee}' is not declared.");
                return TypeKind.Unknown;
            }

            if (call.Arguments.Count != function.Parameters.Count)
            {
                errors.Add($"[Type Error] Function '{call.Callee}' expects {function.Parameters.Count} arguments, but got {call.Arguments.Count}.");
            }

            for (int i = 0; i < Math.Min(call.Arguments.Count, function.Parameters.Count); i++)
            {
                TypeKind argType = VisitExpression(call.Arguments[i]);
                TypeKind paramType = function.Parameters[i].Type;

                if (argType != TypeKind.Unknown && argType != paramType)
                {
                    errors.Add($"[Type Error] Argument {i + 1} of '{call.Callee}' expects {TypeName(paramType)}, but got {TypeName(argType)}.");
                }
            }

            return function.ReturnType;
        }

        private void RegisterVariable(VarStatement var)
        {
            TypeKind initializerType = TypeKind.Unknown;

            if (var.Initializer is not null)
            {
                initializerType = VisitExpression(var.Initializer);
            }

            if (var.DeclaredType is null)
            {
                errors.Add($"[Type Error] Variable '{var.Name}' must have an explicit type annotation in strict static mode.");
                variableTypes[var.Name] = TypeKind.Unknown;
                return;
            }

            TypeKind declaredType = var.DeclaredType.Value;

            if (initializerType != TypeKind.Unknown && initializerType != declaredType)
            {
                errors.Add($"[Type Error] Cannot initialize variable '{var.Name}' of type {TypeName(declaredType)} with {TypeName(initializerType)}.");
            }

            variableTypes[var.Name] = declaredType;
        }

        private TypeKind CheckUnary(UnaryExpression unary)
        {
            TypeKind rightType = VisitExpression(unary.Right);

            if (unary.Operator == TokenType.MINUS)
            {
                if (rightType != TypeKind.Unknown && rightType != TypeKind.Number)
                {
                    errors.Add($"[Type Error] Unary '-' expects number, got {TypeName(rightType)}.");
                }

                return TypeKind.Number;
            }

            if (unary.Operator == TokenType.EXCL)
            {
                if (rightType != TypeKind.Unknown && rightType != TypeKind.Bool)
                {
                    errors.Add($"[Type Error] Unary '!' expects bool, got {TypeName(rightType)}.");
                }

                return TypeKind.Bool;
            }

            return TypeKind.Unknown;
        }

        private TypeKind CheckBinary(BinaryExpression binary)
        {
            TypeKind leftType = VisitExpression(binary.Left);
            TypeKind rightType = VisitExpression(binary.Right);

            switch (binary.Operator)
            {
                case TokenType.PLUS:
                    if (BothKnown(leftType, rightType))
                    {
                        if (leftType == TypeKind.Number && rightType == TypeKind.Number)
                        {
                            return TypeKind.Number;
                        }

                        if (leftType == TypeKind.String && rightType == TypeKind.String)
                        {
                            return TypeKind.String;
                        }

                        errors.Add($"[Type Error] Operator '+' expects number+number or string+string, got {TypeName(leftType)}+{TypeName(rightType)}.");
                    }

                    return TypeKind.Unknown;

                case TokenType.MINUS:
                case TokenType.STAR:
                case TokenType.SLASH:
                    if (BothKnown(leftType, rightType) && (leftType != TypeKind.Number || rightType != TypeKind.Number))
                    {
                        errors.Add($"[Type Error] Operator '{OperatorText(binary.Operator)}' expects number operands, got {TypeName(leftType)} and {TypeName(rightType)}.");
                    }

                    return TypeKind.Number;

                case TokenType.LT:
                case TokenType.LTEQ:
                case TokenType.GT:
                case TokenType.GTEQ:
                    if (BothKnown(leftType, rightType) && (leftType != TypeKind.Number || rightType != TypeKind.Number))
                    {
                        errors.Add($"[Type Error] Operator '{OperatorText(binary.Operator)}' expects number operands, got {TypeName(leftType)} and {TypeName(rightType)}.");
                    }

                    return TypeKind.Bool;

                case TokenType.EQEQ:
                case TokenType.NEQ:
                    if (BothKnown(leftType, rightType) && leftType != rightType)
                    {
                        errors.Add($"[Type Error] Cannot compare values of different types: {TypeName(leftType)} and {TypeName(rightType)}.");
                    }

                    return TypeKind.Bool;

                case TokenType.AND:
                case TokenType.OR:
                    if (BothKnown(leftType, rightType) && (leftType != TypeKind.Bool || rightType != TypeKind.Bool))
                    {
                        errors.Add($"[Type Error] Operator '{OperatorText(binary.Operator)}' expects bool operands, got {TypeName(leftType)} and {TypeName(rightType)}.");
                    }

                    return TypeKind.Bool;

                default:
                    return TypeKind.Unknown;
            }
        }

        private void EnsureBooleanCondition(TypeKind conditionType, string construct)
        {
            if (conditionType != TypeKind.Unknown && conditionType != TypeKind.Bool)
            {
                errors.Add($"[Type Error] Condition of '{construct}' must be bool, got {TypeName(conditionType)}.");
            }
        }

        private void MergeBranchTypes(
            Dictionary<string, TypeKind> first,
            Dictionary<string, TypeKind> second)
        {
            var merged = new Dictionary<string, TypeKind>();

            foreach (var name in first.Keys.Union(second.Keys))
            {
                bool hasFirst = first.TryGetValue(name, out var firstType);
                bool hasSecond = second.TryGetValue(name, out var secondType);

                if (hasFirst && hasSecond)
                {
                    merged[name] = JoinTypes(firstType, secondType);
                }
                else if (hasFirst)
                {
                    merged[name] = firstType;
                }
                else if (hasSecond)
                {
                    merged[name] = secondType;
                }
            }

            variableTypes.Clear();
            foreach (var kv in merged)
            {
                variableTypes[kv.Key] = kv.Value;
            }
        }

        private static TypeKind JoinTypes(TypeKind first, TypeKind second)
        {
            if (first == second)
            {
                return first;
            }

            if (first == TypeKind.Unknown)
            {
                return second;
            }

            if (second == TypeKind.Unknown)
            {
                return first;
            }

            return TypeKind.Unknown;
        }

        private static bool BothKnown(TypeKind left, TypeKind right)
        {
            return left != TypeKind.Unknown && right != TypeKind.Unknown;
        }

        private static Dictionary<string, TypeKind> Copy(Dictionary<string, TypeKind> source)
        {
            return new Dictionary<string, TypeKind>(source);
        }

        private static string TypeName(TypeKind type)
        {
            return type.ToString().ToLowerInvariant();
        }

        private static string OperatorText(TokenType type)
        {
            return type switch
            {
                TokenType.PLUS => "+",
                TokenType.MINUS => "-",
                TokenType.STAR => "*",
                TokenType.SLASH => "/",
                TokenType.LT => "<",
                TokenType.LTEQ => "<=",
                TokenType.GT => ">",
                TokenType.GTEQ => ">=",
                TokenType.EQEQ => "==",
                TokenType.NEQ => "!=",
                TokenType.AND => "&&",
                TokenType.OR => "||",
                _ => type.ToString()
            };
        }
    }
}
