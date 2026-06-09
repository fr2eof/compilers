using Lexer;

Console.WriteLine("=== Учебный Компилятор ===");

// Исходный код на нашем языке
string testCode = @"
var arr: array = [10, 20, 30];
print(""Initial array:"");
print(arr);

arr[1] = 50;
print(""After setting arr[1] = 50:"");
print(arr);

var sum: number = arr[0] + arr[1];
print(""Sum of arr[0] and arr[1]:"");
print(sum);
";

Console.WriteLine("Исходный код:");
Console.WriteLine(testCode);

// ==============================================================================
// ШАГ 1: ЛЕКСИЧЕСКИЙ АНАЛИЗ (Lexer)
// ==============================================================================
Console.WriteLine("\n=== 1. Токены (Lexer) ===\n");
var lexer = new Lexer.Lexer(testCode);
var tokens = lexer.Tokenize();

// Выводим результат токенизации (превращения текста в слова).
foreach (var token in tokens)
{
    Console.WriteLine(token);
}


// ==============================================================================
// ШАГ 2: СИНТАКСИЧЕСКИЙ АНАЛИЗ (Parser / AST)
// ==============================================================================
Console.WriteLine("\n=== 2. Дерево (Parser / AST) ===\n");
var parser = new Parser(tokens);
List<Statement> statements;
try
{
    statements = parser.Parse();
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка синтаксиса: {ex.Message}");
    return;
}

// Показываем дерево до оптимизаций:
var printer = new AstPrinter();
printer.Print(statements);


// ==============================================================================
// ШАГ 3: ОПТИМИЗАЦИЯ КОДА (Constant Folding + Dead Code Elimination)
// ==============================================================================
Console.WriteLine("\n=== 3. Оптимизация дерева (Свертка констант) ===\n");
var constantFolder = new ConstantFolder();
var foldedAst = constantFolder.Optimize(statements);

Console.WriteLine("После свертки литералов:");
printer.Print(foldedAst);

Console.WriteLine("\n=== 3.1 Оптимизация дерева (DCE) ===\n");
var dce = new DeadCodeEliminator();
var optimizedAst = dce.Optimize(foldedAst); // Вырезаем недостижимый код!

Console.WriteLine("После удаления мертвого кода:");
printer.Print(optimizedAst);


// ==============================================================================
// ШАГ 4: СЕМАНТИЧЕСКИЙ АНАЛИЗ (Semantic)
// ==============================================================================
Console.WriteLine("\n=== 4. Семантические проверки ===\n");

// 4.1 Проверка на неиспользуемые переменные
var unusedVarChecker = new UnusedVariableChecker();
unusedVarChecker.Check(optimizedAst);
if (unusedVarChecker.Warnings.Count == 0) Console.WriteLine("- Нет неиспользуемых переменных.");
foreach (var warning in unusedVarChecker.Warnings) Console.WriteLine($"Внимание: {warning}");

// 4.2 Проверка на чтение неинициализированных данных
var uninitChecker = new UninitializedVariableChecker();
uninitChecker.Check(optimizedAst);
if (uninitChecker.Errors.Count == 0) Console.WriteLine("- Нет чтения пустых переменных.");
foreach (var error in uninitChecker.Errors) Console.WriteLine($"Ошибка: {error}");

// 4.3 Проверка типов функций и выражений
var typeChecker = new TypeChecker();
typeChecker.Check(optimizedAst);
if (typeChecker.Errors.Count > 0)
{
    foreach (var error in typeChecker.Errors) Console.WriteLine($"Ошибка типа: {error}");
    return; // Останавливаем компилятор, код содержит ошибку типов
}
Console.WriteLine("- Проверка типов пройдена.");


// ==============================================================================
// ШАГ 5: ПРОМЕЖУТОЧНОЕ ПРЕДСТАВЛЕНИЕ (IR / LLMR)
// ==============================================================================
Console.WriteLine("\n=== 5. LLMR (Линейный ассемблероподобный код) ===\n");
var translator = new LlmrTranslator();
var llmrInstructions = translator.Translate(optimizedAst); // Конвертируем Дерево -> В список инструкций

foreach (var inst in llmrInstructions)
{
    Console.WriteLine(inst);
}


// ==============================================================================
// ШАГ 6: ВЫПОЛНЕНИЕ (Interpreter)
// ==============================================================================
Console.WriteLine("\n=== 6. Результат выполнения (Интерпретатор) ===\n");
var interpreter = new Interpreter();
try
{
    interpreter.Execute(optimizedAst);
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка выполнения: {ex.Message}");
}
