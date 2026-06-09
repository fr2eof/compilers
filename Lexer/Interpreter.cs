using System.Globalization;

namespace Lexer
{
    public class ReturnValueException : Exception
    {
        public object? Value { get; }
        public ReturnValueException(object? value) { Value = value; }
    }

    public sealed class Interpreter
    {
        private readonly RuntimeEnvironment environment;

        public Interpreter(RuntimeEnvironment? environment = null)
        {
            this.environment = environment ?? new RuntimeEnvironment();
        }

        public void Execute(IEnumerable<Statement> statements)
        {
            foreach (Statement statement in statements)
            {
                Execute(statement);
            }
        }

        public void Execute(Statement statement)
        {
            switch (statement)
            {
                case ExpressionStatement expressionStatement:
                    Eval(expressionStatement.Expression);
                    break;

                case PrintStatement printStatement:
                    Console.WriteLine(FormatValue(Eval(printStatement.Expression)));
                    break;

                case VarStatement varStatement:
                    ExecuteVarStatement(varStatement);
                    break;

                case BlockStatement blockStatement:
                    ExecuteBlock(blockStatement.Statements);
                    break;

                case IfStatement conditional:
                    if (RequireBoolean(Eval(conditional.Condition), "if condition"))
                    {
                        Execute(conditional.ThenBranch);
                    }
                    else if (conditional.ElseBranch is not null)
                    {
                        Execute(conditional.ElseBranch);
                    }

                    break;

                case WhileStatement loop:
                    while (RequireBoolean(Eval(loop.Condition), "while condition"))
                    {
                        Execute(loop.Body);
                    }
                    break;

                case FunctionStatement function:
                    environment.Declare(function.Name, function, true);
                    break;

                case ReturnStatement returnStmt:
                    object? value = null;
                    if (returnStmt.Value is not null)
                    {
                        value = Eval(returnStmt.Value);
                    }
                    throw new ReturnValueException(value);

                default:
                    throw new InvalidOperationException($"[Runtime Error] Unsupported statement: {statement.GetType().Name}.");
            }
        }

        public object? Eval(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression numberExpression:
                    return numberExpression.Value;

                case StringExpression stringExpression:
                    return stringExpression.Value;

                case BooleanExpression booleanExpression:
                    return booleanExpression.Value;

                case VariableExpression variableExpression:
                    return environment.Get(variableExpression.Name);

                case AssignExpression assignExpression:
                    object? assignedValue = Eval(assignExpression.Value);
                    environment.Assign(assignExpression.Name, assignedValue);
                    return assignedValue;

                case ArrayExpression arrayExpr:
                    var arrayValues = new List<object?>();
                    foreach (var elem in arrayExpr.Elements)
                    {
                        arrayValues.Add(Eval(elem));
                    }
                    return arrayValues;

                case IndexExpression indexExpr:
                    var srcArray = Eval(indexExpr.Array) as List<object?>;
                    if (srcArray == null) throw new InvalidOperationException("[Runtime Error] Trying to index a non-array.");
                    var getIdxObj = Eval(indexExpr.Index);
                    if (getIdxObj is not double getIdxStr) throw new InvalidOperationException("[Runtime Error] Array index must be a number.");
                    int getIdx = (int)getIdxStr;
                    if (getIdx < 0 || getIdx >= srcArray.Count) throw new InvalidOperationException($"[Runtime Error] Index out of bounds: {getIdx}");
                    return srcArray[getIdx];

                case IndexAssignExpression idxAssign:
                    var dstArray = Eval(idxAssign.Array) as List<object?>;
                    if (dstArray == null) throw new InvalidOperationException("[Runtime Error] Trying to assign to a non-array.");
                    var setIdxObj = Eval(idxAssign.Index);
                    if (setIdxObj is not double setIdxStr) throw new InvalidOperationException("[Runtime Error] Array index must be a number.");
                    int setIdx = (int)setIdxStr;
                    if (setIdx < 0 || setIdx >= dstArray.Count) throw new InvalidOperationException($"[Runtime Error] Index out of bounds: {setIdx}");
                    var valToSet = Eval(idxAssign.Value);
                    dstArray[setIdx] = valToSet;
                    return valToSet;

                case CallExpression callExpression:
                    return EvalCall(callExpression);

                case UnaryExpression unaryExpression:
                    return EvalUnary(unaryExpression);

                case BinaryExpression binaryExpression:
                    return EvalBinary(binaryExpression);

                default:
                    throw new InvalidOperationException($"[Runtime Error] Unsupported expression: {expression.GetType().Name}.");
            }
        }

        private void ExecuteVarStatement(VarStatement varStatement)
        {
            if (varStatement.Initializer is not null)
            {
                object? initializerValue = Eval(varStatement.Initializer);
                environment.Declare(varStatement.Name, initializerValue, true);
                return;
            }

            environment.Declare(varStatement.Name);
        }

        private void ExecuteBlock(IEnumerable<Statement> statements)
        {
            environment.PushScope();

            try
            {
                foreach (Statement statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                environment.PopScope();
            }
        }

        private object? EvalUnary(UnaryExpression unaryExpression)
        {
            object? right = Eval(unaryExpression.Right);

            return unaryExpression.Operator switch
            {
                TokenType.MINUS => -RequireNumber(right, "unary '-'"),
                TokenType.EXCL => !RequireBoolean(right, "unary '!'"),
                _ => throw new InvalidOperationException($"[Runtime Error] Unsupported unary operator '{unaryExpression.Operator}'."),
            };
        }

        private object? EvalCall(CallExpression callExpression)
        {
            var functionValue = environment.Get(callExpression.Callee);
            if (functionValue is not FunctionStatement function)
            {
                throw new InvalidOperationException($"[Runtime Error] '{callExpression.Callee}' is not a function.");
            }

            var args = new List<object?>();
            foreach (var argExpr in callExpression.Arguments)
            {
                args.Add(Eval(argExpr));
            }

            environment.PushScope();
            try
            {
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    environment.Declare(function.Parameters[i].Name, args[i], true);
                }

                Execute(function.Body);
            }
            catch (ReturnValueException ret)
            {
                return ret.Value;
            }
            finally
            {
                environment.PopScope();
            }

            return null;
        }

        private object? EvalBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression.Operator == TokenType.AND)
            {
                bool left = RequireBoolean(Eval(binaryExpression.Left), "'&&' left operand");
                if (!left)
                {
                    return false;
                }

                return RequireBoolean(Eval(binaryExpression.Right), "'&&' right operand");
            }

            if (binaryExpression.Operator == TokenType.OR)
            {
                bool left = RequireBoolean(Eval(binaryExpression.Left), "'||' left operand");
                if (left)
                {
                    return true;
                }

                return RequireBoolean(Eval(binaryExpression.Right), "'||' right operand");
            }

            object? leftValue = Eval(binaryExpression.Left);
            object? rightValue = Eval(binaryExpression.Right);

            return binaryExpression.Operator switch
            {
                TokenType.PLUS => EvalPlus(leftValue, rightValue),
                TokenType.MINUS => RequireNumber(leftValue, "'-' left operand") - RequireNumber(rightValue, "'-' right operand"),
                TokenType.STAR => RequireNumber(leftValue, "'*' left operand") * RequireNumber(rightValue, "'*' right operand"),
                TokenType.SLASH => RequireNumber(leftValue, "'/' left operand") / RequireNumber(rightValue, "'/' right operand"),
                TokenType.LT => RequireNumber(leftValue, "'<' left operand") < RequireNumber(rightValue, "'<' right operand"),
                TokenType.LTEQ => RequireNumber(leftValue, "'<=' left operand") <= RequireNumber(rightValue, "'<=' right operand"),
                TokenType.GT => RequireNumber(leftValue, "'>' left operand") > RequireNumber(rightValue, "'>' right operand"),
                TokenType.GTEQ => RequireNumber(leftValue, "'>=' left operand") >= RequireNumber(rightValue, "'>=' right operand"),
                TokenType.EQEQ => Equals(leftValue, rightValue),
                TokenType.NEQ => !Equals(leftValue, rightValue),
                _ => throw new InvalidOperationException($"[Runtime Error] Unsupported binary operator '{binaryExpression.Operator}'."),
            };
        }

        private static object EvalPlus(object? left, object? right)
        {
            if (left is double leftNumber && right is double rightNumber)
            {
                return leftNumber + rightNumber;
            }

            if (left is string leftString && right is string rightString)
            {
                return leftString + rightString;
            }

            throw new InvalidOperationException($"[Runtime Error] Operator '+' expects number+number or string+string, got {DescribeValue(left)}+{DescribeValue(right)}.");
        }

        private static double RequireNumber(object? value, string context)
        {
            if (value is double number)
            {
                return number;
            }

            throw new InvalidOperationException($"[Runtime Error] {context} expects a number, got {DescribeValue(value)}.");
        }

        private static bool RequireBoolean(object? value, string context)
        {
            if (value is bool boolean)
            {
                return boolean;
            }

            throw new InvalidOperationException($"[Runtime Error] {context} expects a boolean, got {DescribeValue(value)}.");
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "null",
                double number => number.ToString(CultureInfo.InvariantCulture),
                bool boolean => boolean ? "true" : "false",
                List<object?> list => "[" + string.Join(", ", list.Select(v => FormatValue(v))) + "]",
                _ => value.ToString() ?? string.Empty,
            };
        }

        private static string DescribeValue(object? value)
        {
            return value switch
            {
                null => "null",
                double => "number",
                string => "string",
                bool => "boolean",
                _ => value.GetType().Name,
            };
        }
    }
}