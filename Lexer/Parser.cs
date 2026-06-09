namespace Lexer
{
    /// <summary>
    /// Шаг 2: Синтаксический Анализ (Парсер)
    /// Принимает плоский список токенов от Lexer и строит из них структурное древо (AST),
    /// опираясь на грамматику языка (например, выражение состоит из слагаемых, а слагаемое из множителей).
    /// </summary>
    public class Parser
    {
        private readonly List<Token> tokens;
        private int position;

        public Parser(IEnumerable<Token> tokens)
        {
            this.tokens = tokens.ToList();
            position = 0;
        }

        public List<Statement> Parse()
        {
            var statements = new List<Statement>();

            while (!IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            return statements;
        }

        private Statement ParseDeclaration()
        {
            if (Match(TokenType.FUN))
            {
                return ParseFunctionStatement();
            }

            if (Match(TokenType.VAR))
            {
                return ParseVarDeclaration();
            }

            return ParseStatement();
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.RETURN))
            {
                return ParseReturnStatement();
            }

            if (Match(TokenType.IF))
            {
                return ParseIfStatement();
            }

            if (Match(TokenType.WHILE))
            {
                return ParseWhileStatement();
            }

            if (Match(TokenType.PRINT))
            {
                return ParsePrintStatement();
            }

            if (Match(TokenType.LBRACE))
            {
                return new BlockStatement(ParseBlock());
            }

            return ParseExpressionStatement();
        }

        private Statement ParseVarDeclaration()
        {
            Token name = Consume(TokenType.ID, "Expected variable name.");
            Consume(TokenType.COLON, "Expected ':' and explicit type after variable name.");
            TypeKind declaredType = ParseTypeAnnotation();

            Expression? initializer = null;

            if (Match(TokenType.EQ))
            {
                initializer = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");
            return new VarStatement(name.Value, declaredType, initializer, name.Position);
        }

        private Statement ParseIfStatement()
        {
            Consume(TokenType.LPAREN, "Expected '(' after 'if'.");
            Expression condition = ParseExpression();
            Consume(TokenType.RPAREN, "Expected ')' after if condition.");

            Statement thenBranch = ParseStatement();
            Statement? elseBranch = null;

            if (Match(TokenType.ELSE))
            {
                elseBranch = ParseStatement();
            }

            return new IfStatement(condition, thenBranch, elseBranch);
        }

        private Statement ParseWhileStatement()
        {
            Consume(TokenType.LPAREN, "Expected '(' after 'while'.");
            Expression condition = ParseExpression();
            Consume(TokenType.RPAREN, "Expected ')' after while condition.");

            Statement body = ParseStatement();
            return new WhileStatement(condition, body);
        }

        private Statement ParseFunctionStatement()
        {
            Token name = Consume(TokenType.ID, "Expected function name.");
            Consume(TokenType.LPAREN, "Expected '(' after function name.");

            List<Parameter> parameters = new List<Parameter>();
            if (!Check(TokenType.RPAREN))
            {
                do
                {
                    Token paramName = Consume(TokenType.ID, "Expected parameter name.");
                    Consume(TokenType.COLON, "Expected ':' after parameter name.");
                    TypeKind paramType = ParseTypeAnnotation();
                    parameters.Add(new Parameter(paramName.Value, paramType));
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RPAREN, "Expected ')' after parameters.");

            Consume(TokenType.COLON, "Expected ':' and return type after function parameters.");
            TypeKind returnType = ParseTypeAnnotation();

            Consume(TokenType.LBRACE, "Expected '{' before function body.");
            Statement body = new BlockStatement(ParseBlock());

            return new FunctionStatement(name.Value, parameters, returnType, body);
        }

        private Statement ParseReturnStatement()
        {
            Expression? value = null;
            if (!Check(TokenType.SEMICOLON))
            {
                value = ParseExpression();
            }
            Consume(TokenType.SEMICOLON, "Expected ';' after return value.");
            return new ReturnStatement(value);
        }

        private Statement ParsePrintStatement()
        {
            Expression value = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after print value.");
            return new PrintStatement(value);
        }

        private Statement ParseExpressionStatement()
        {
            Expression expression = ParseExpression();
            Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
            return new ExpressionStatement(expression);
        }

        private List<Statement> ParseBlock()
        {
            var statements = new List<Statement>();

            while (!Check(TokenType.RBRACE) && !IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RBRACE, "Expected '}' after block.");
            return statements;
        }

        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        private Expression ParseAssignment()
        {
            Expression expression = ParseLogicalOr();

            if (Match(TokenType.EQ))
            {
                Token equals = Previous();
                Expression value = ParseAssignment();

                if (expression is VariableExpression variable)
                {
                    return new AssignExpression(variable.Name, value);
                }

                throw Error(equals, "Invalid assignment target.");
            }

            return expression;
        }

        private Expression ParseLogicalOr()
        {
            Expression expression = ParseLogicalAnd();

            while (Match(TokenType.OR))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseLogicalAnd();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseLogicalAnd()
        {
            Expression expression = ParseEquality();

            while (Match(TokenType.AND))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseEquality();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseEquality()
        {
            Expression expression = ParseComparison();

            while (Match(TokenType.EQEQ, TokenType.NEQ))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseComparison();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseComparison()
        {
            Expression expression = ParseTerm();

            while (Match(TokenType.LT, TokenType.LTEQ, TokenType.GT, TokenType.GTEQ))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseTerm();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseTerm()
        {
            Expression expression = ParseFactor();

            while (Match(TokenType.PLUS, TokenType.MINUS))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseFactor();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseFactor()
        {
            Expression expression = ParseUnary();

            while (Match(TokenType.STAR, TokenType.SLASH))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseUnary();
                expression = new BinaryExpression(expression, @operator, right);
            }

            return expression;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.EXCL, TokenType.MINUS))
            {
                TokenType @operator = Previous().Type;
                Expression right = ParseUnary();
                return new UnaryExpression(@operator, right);
            }

            return ParseCall();
        }

        private Expression ParseCall()
        {
            Expression expr = ParsePrimary();

            if (expr is VariableExpression variable && Match(TokenType.LPAREN))
            {
                List<Expression> arguments = new List<Expression>();
                if (!Check(TokenType.RPAREN))
                {
                    do
                    {
                        arguments.Add(ParseExpression());
                    } while (Match(TokenType.COMMA));
                }
                Consume(TokenType.RPAREN, "Expected ')' after arguments.");
                return new CallExpression(variable.Name, arguments);
            }

            return expr;
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenType.NUMBER))
            {
                double value = double.Parse(Previous().Value, System.Globalization.CultureInfo.InvariantCulture);
                return new NumberExpression(value);
            }

            if (Match(TokenType.TRUE))
            {
                return new BooleanExpression(true);
            }

            if (Match(TokenType.FALSE))
            {
                return new BooleanExpression(false);
            }

            if (Match(TokenType.STRING))
            {
                string value = Previous().Value;
                if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                {
                    value = value[1..^1];
                }

                return new StringExpression(value);
            }

            if (Match(TokenType.ID))
            {
                return new VariableExpression(Previous().Value);
            }

            if (Match(TokenType.LPAREN))
            {
                Expression expression = ParseExpression();
                Consume(TokenType.RPAREN, "Expected ')' after expression.");
                return expression;
            }

            throw Error(Peek(), "Expected expression.");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd())
            {
                return false;
            }

            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd())
            {
                position++;
            }

            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[position];
        }

        private Token Previous()
        {
            return tokens[position - 1];
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type))
            {
                return Advance();
            }

            throw Error(Peek(), message);
        }

        private TypeKind ParseTypeAnnotation()
        {
            if (Match(TokenType.TYPE_NUMBER))
            {
                return TypeKind.Number;
            }

            if (Match(TokenType.TYPE_STRING))
            {
                return TypeKind.String;
            }

            if (Match(TokenType.TYPE_BOOLEAN))
            {
                return TypeKind.Bool;
            }

            throw Error(Peek(), "Expected type name 'number', 'string' or 'boolean'.");
        }

        private static Exception Error(Token token, string message)
        {
            return new InvalidOperationException($"[Parser Error] Position {token.Position}: {message}");
        }
    }
}