using System.Collections.Generic;

namespace Lexer
{
    /// <summary>
    /// Шаг 5: Промежуточное представление (IR / Low-Level Machine Representation).
    /// Эта утилита берет наше Дерево (AST) и "выпрямляет" его в плоский список инструкций (LLMR),
    /// похожий на ассемблер (трехадресный код). 
    /// Этот код гораздо проще оптимизировать или перевести в настоящий x86/ARM байткод.
    /// </summary>
    public class LlmrTranslator
    {
        private int labelCounter = 0;
        private int tempCounter = 0;

        private string NewTemp() => $"%t{tempCounter++}";
        private string NewLabel(string prefix) => $".{prefix}_{labelCounter++}";

        public List<LlmrInstruction> Translate(IEnumerable<Statement> statements)
        {
            var instructions = new List<LlmrInstruction>();
            foreach (var stmt in statements)
            {
                TranslateStatement(stmt, instructions);
            }
            return instructions;
        }

        private void TranslateStatement(Statement statement, List<LlmrInstruction> output)
        {
            switch (statement)
            {
                case BlockStatement block:
                    foreach (var stmt in block.Statements)
                        TranslateStatement(stmt, output);
                    break;

                case FunctionStatement func:
                    output.Add(new LabelInstruction($"func_{func.Name}"));
                    TranslateStatement(func.Body, output);
                    output.Add(new ReturnInstruction("0")); // Default return if none provided
                    break;

                case ReturnStatement ret:
                    if (ret.Value != null)
                    {
                        var val = TranslateExpression(ret.Value, output);
                        output.Add(new ReturnInstruction(val));
                    }
                    else
                    {
                        output.Add(new ReturnInstruction("0"));
                    }
                    break;

                case PrintStatement p:
                    var valToPrint = TranslateExpression(p.Expression, output);
                    output.Add(new PrintInstruction(valToPrint));
                    break;

                case VarStatement varStmt:
                    if (varStmt.Initializer != null)
                    {
                        var initVal = TranslateExpression(varStmt.Initializer, output);
                        output.Add(new AssignmentInstruction(varStmt.Name, initVal));
                    }
                    break;

                case IfStatement ifStmt:
                    var cond = TranslateExpression(ifStmt.Condition, output);
                    var endLabel = NewLabel("endif");
                    var elseLabel = ifStmt.ElseBranch != null ? NewLabel("else") : endLabel;

                    output.Add(new JumpIfFalseInstruction(cond, elseLabel));

                    TranslateStatement(ifStmt.ThenBranch, output);
                    output.Add(new JumpInstruction(endLabel));

                    if (ifStmt.ElseBranch != null)
                    {
                        output.Add(new LabelInstruction(elseLabel));
                        TranslateStatement(ifStmt.ElseBranch, output);
                    }

                    output.Add(new LabelInstruction(endLabel));
                    break;

                case WhileStatement whileStmt:
                    var loopStart = NewLabel("loop_start");
                    var loopEnd = NewLabel("loop_end");

                    output.Add(new LabelInstruction(loopStart));
                    var loopCond = TranslateExpression(whileStmt.Condition, output);
                    output.Add(new JumpIfFalseInstruction(loopCond, loopEnd));

                    TranslateStatement(whileStmt.Body, output);
                    output.Add(new JumpInstruction(loopStart));

                    output.Add(new LabelInstruction(loopEnd));
                    break;

                case ExpressionStatement exprStmt:
                    TranslateExpression(exprStmt.Expression, output);
                    break;
            }
        }

        private string TranslateExpression(Expression expr, List<LlmrInstruction> output)
        {
            switch (expr)
            {
                case NumberExpression numExp:
                    return numExp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                case StringExpression strExp:
                    return $"\"{strExp.Value}\"";

                case BooleanExpression boolExp:
                    return boolExp.Value ? "true" : "false";

                case VariableExpression varExp:
                    return varExp.Name;

                case AssignExpression assignExp:
                    var rhs = TranslateExpression(assignExp.Value, output);
                    output.Add(new AssignmentInstruction(assignExp.Name, rhs));
                    return assignExp.Name;
                case ArrayExpression arrayExpr:
                    var arrayTemp = NewTemp();
                    output.Add(new AssignmentInstruction(arrayTemp, "[array]"));
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        var eStr = TranslateExpression(arrayExpr.Elements[i], output);
                        output.Add(new AssignmentInstruction($"{arrayTemp}[{i}]", eStr));
                    }
                    return arrayTemp;

                case IndexExpression indexExpr:
                    var srcStr = TranslateExpression(indexExpr.Array, output);
                    var idxStr = TranslateExpression(indexExpr.Index, output);
                    var tempStr = NewTemp();
                    output.Add(new AssignmentInstruction(tempStr, $"{srcStr}[{idxStr}]"));
                    return tempStr;

                case IndexAssignExpression idxAssign:
                    var dstStr = TranslateExpression(idxAssign.Array, output);
                    var idxSetStr = TranslateExpression(idxAssign.Index, output);
                    var valStr = TranslateExpression(idxAssign.Value, output);
                    output.Add(new AssignmentInstruction($"{dstStr}[{idxSetStr}]", valStr));
                    return valStr;
                case BinaryExpression binExp:
                    var left = TranslateExpression(binExp.Left, output);
                    var right = TranslateExpression(binExp.Right, output);
                    var result = NewTemp();
                    output.Add(new BinaryInstruction(result, left, OperatorToString(binExp.Operator), right));
                    return result;

                case UnaryExpression unExp:
                    var operand = TranslateExpression(unExp.Right, output);
                    var unaryRes = NewTemp();
                    output.Add(new AssignmentInstruction(unaryRes, $"{OperatorToString(unExp.Operator)}{operand}"));
                    return unaryRes;

                case CallExpression callExp:
                    var args = new List<string>();
                    foreach (var arg in callExp.Arguments)
                    {
                        args.Add(TranslateExpression(arg, output));
                    }
                    var callRes = NewTemp();
                    output.Add(new CallInstruction(callRes, $"func_{callExp.Callee}", args));
                    return callRes;

                default:
                    throw new System.Exception("Unknown expression in LLMR translator.");
            }
        }

        private string OperatorToString(TokenType op)
        {
            return op switch
            {
                TokenType.PLUS => "+",
                TokenType.MINUS => "-",
                TokenType.STAR => "*",
                TokenType.SLASH => "/",
                TokenType.EQEQ => "==",
                TokenType.NEQ => "!=",
                TokenType.LT => "<",
                TokenType.LTEQ => "<=",
                TokenType.GT => ">",
                TokenType.GTEQ => ">=",
                TokenType.AND => "&&",
                TokenType.OR => "||",
                TokenType.EXCL => "!",
                _ => op.ToString()
            };
        }
    }
}
