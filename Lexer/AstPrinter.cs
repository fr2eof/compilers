namespace Lexer
{
    public class AstPrinter
    {
        public void Print(List<Statement> statements)
        {
            Console.WriteLine("Root (Program)");

            for (int index = 0; index < statements.Count; index++)
            {
                PrintNode(statements[index], string.Empty, index == statements.Count - 1);
            }
        }

        private void PrintNode(object? node, string indent, bool isLast)
        {
            if (node is null)
            {
                return;
            }

            string marker = isLast ? "└── " : "├── ";
            Console.Write(indent + marker);

            string childIndent = indent + (isLast ? "    " : "│   ");

            switch (node)
            {
                case VarStatement variable:
                    if (variable.DeclaredType is not null)
                    {
                        Console.WriteLine($"VarStatement: {variable.Name} : {TypeName(variable.DeclaredType.Value)}");
                    }
                    else
                    {
                        Console.WriteLine($"VarStatement: {variable.Name}");
                    }

                    if (variable.Initializer is not null)
                    {
                        PrintNode(variable.Initializer, childIndent, true);
                    }
                    break;

                case PrintStatement print:
                    Console.WriteLine("PrintStatement");
                    PrintNode(print.Expression, childIndent, true);
                    break;

                case IfStatement conditional:
                    Console.WriteLine("IfStatement");
                    PrintNode(conditional.Condition, childIndent, false);
                    PrintNode(conditional.ThenBranch, childIndent, conditional.ElseBranch is null);
                    if (conditional.ElseBranch is not null)
                    {
                        PrintNode(conditional.ElseBranch, childIndent, true);
                    }
                    break;

                case WhileStatement loop:
                    Console.WriteLine("WhileStatement");
                    PrintNode(loop.Condition, childIndent, false);
                    PrintNode(loop.Body, childIndent, true);
                    break;

                case BlockStatement block:
                    Console.WriteLine("BlockStatement");
                    for (int index = 0; index < block.Statements.Count; index++)
                    {
                        PrintNode(block.Statements[index], childIndent, index == block.Statements.Count - 1);
                    }
                    break;

                case FunctionStatement functionStmt:
                    string paramsStr = string.Join(", ", functionStmt.Parameters.Select(p => $"{p.Name}: {TypeName(p.Type)}"));
                    Console.WriteLine($"FunctionStatement: {functionStmt.Name}({paramsStr}) : {TypeName(functionStmt.ReturnType)}");
                    PrintNode(functionStmt.Body, childIndent, true);
                    break;

                case ReturnStatement returnStmt:
                    Console.WriteLine("ReturnStatement");
                    if (returnStmt.Value is not null)
                    {
                        PrintNode(returnStmt.Value, childIndent, true);
                    }
                    break;

                case CallExpression call:
                    Console.WriteLine($"CallExpression: {call.Callee}");
                    for (int i = 0; i < call.Arguments.Count; i++)
                    {
                        PrintNode(call.Arguments[i], childIndent, i == call.Arguments.Count - 1);
                    }
                    break;

                case ExpressionStatement expressionStatement:
                    Console.WriteLine("ExpressionStatement");
                    PrintNode(expressionStatement.Expression, childIndent, true);
                    break;

                case BinaryExpression binary:
                    Console.WriteLine($"BinaryExpression: {binary.Operator}");
                    PrintNode(binary.Left, childIndent, false);
                    PrintNode(binary.Right, childIndent, true);
                    break;

                case UnaryExpression unary:
                    Console.WriteLine($"UnaryExpression: {unary.Operator}");
                    PrintNode(unary.Right, childIndent, true);
                    break;

                case AssignExpression assignment:
                    Console.WriteLine($"AssignExpression: {assignment.Name}");
                    PrintNode(assignment.Value, childIndent, true);
                    break;

                case ArrayExpression arrayExpr:
                    Console.WriteLine($"ArrayExpression");
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        PrintNode(arrayExpr.Elements[i], childIndent, i == arrayExpr.Elements.Count - 1);
                    }
                    break;

                case IndexExpression indexExpr:
                    Console.WriteLine($"IndexExpression");
                    PrintNode(indexExpr.Array, childIndent, false);
                    PrintNode(indexExpr.Index, childIndent, true);
                    break;

                case IndexAssignExpression idxAssign:
                    Console.WriteLine($"IndexAssignExpression");
                    PrintNode(idxAssign.Array, childIndent, false);
                    PrintNode(idxAssign.Index, childIndent, false);
                    PrintNode(idxAssign.Value, childIndent, true);
                    break;

                case NumberExpression number:
                    Console.WriteLine($"Number: {number.Value}");
                    break;

                case StringExpression text:
                    Console.WriteLine($"String: \"{text.Value}\"");
                    break;

                case BooleanExpression boolean:
                    Console.WriteLine($"Boolean: {boolean.Value.ToString().ToLowerInvariant()}");
                    break;

                case VariableExpression variableExpression:
                    Console.WriteLine($"Variable: {variableExpression.Name}");
                    break;

                default:
                    Console.WriteLine($"Unknown Node: {node.GetType().Name}");
                    break;
            }
        }

        private static string TypeName(TypeKind type)
        {
            return type switch
            {
                TypeKind.Number => "number",
                TypeKind.String => "string",
                TypeKind.Bool => "boolean",
                _ => "unknown"
            };
        }
    }
}