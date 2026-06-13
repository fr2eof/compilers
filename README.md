# Учебный компилятор

Минимальный компилятор и интерпретатор на C#, реализующий полный цикл обработки собственного языка программирования: от лексического анализа исходного текста до выполнения программы виртуальной машиной.

Проект построен по классической архитектуре компилятора:

```

Source Code
|
v
Lexer
|
v
Parser
|
v
AST
|
v
Optimization (Middle-end)
|
v
Semantic Analysis
|
v
LLMR Intermediate Representation
|
v
Runtime / Interpreter
|
v
Program Output

```

---

# Возможности

## Лексический анализ

Поддерживается преобразование исходного текста программы в поток токенов.

Возможности:

- распознавание ключевых слов
- идентификаторы
- числа
- строки
- логические значения
- арифметические операторы
- логические операторы
- операторы сравнения
- массивы
- комментарии
- пропуск пробелов

Используется алгоритм:

**Maximal Munch (жадное поглощение символов)**

Результат:

```

source code
↓
Token[]

```

---

# Синтаксический анализ

Parser преобразует поток токенов в дерево программы (AST).

Используется:

- Recursive Descent Parser
- обработка приоритета операторов
- обработка ассоциативности

Поддерживаются:

## Expressions

- числа
- строки
- boolean
- бинарные операции
- унарные операции
- вызовы функций
- массивы
- индексация


## Statements

- объявления переменных
- присваивание
- if
- while
- return
- функции
- блоки кода

---

# AST (Abstract Syntax Tree)

Все конструкции языка представлены деревом узлов.

Примеры узлов:

```

BinaryExpression
UnaryExpression

VariableExpression
CallExpression

ArrayExpression
IndexExpression
IndexAssignExpression

IfStatement
WhileStatement
FunctionStatement
ReturnStatement

```

AST является основным представлением программы между этапами компиляции.

---

# Семантический анализ

Перед выполнением программа проходит статическую проверку.

Реализовано:

## Таблица символов

Хранение:

- переменных
- функций
- типов
- областей видимости


## Проверка типов

Поддерживается контроль:

- операций над совместимыми типами
- типов переменных
- типов аргументов функций
- типа возвращаемого значения
- типов массивов


Примеры ошибок:

```

number + string

unknown variable

wrong function argument type

wrong return type

```


## Проверка переменных

Добавлены проверки:

- использование необъявленных переменных
- использование неинициализированных переменных
- объявленные, но неиспользованные переменные


Используется паттерн:

```

Visitor

```
---

# Структура проекта

```

Compiler/

├── Lexer.cs
├── Token.cs
├── TokenType.cs

├── Parser.cs
├── Expression.cs
├── Statement.cs

├── TypeChecker.cs
├── UnusedVariableChecker.cs
├── UninitializedVariableChecker.cs

├── ConstantFolder.cs
├── DeadCodeEliminator.cs

├── LlmrTranslator.cs
├── LlmrInstruction.cs

├── Interpreter.cs
├── RuntimeEnvironment.cs

└── Program.cs

```

---

# Статус лабораторных работ

| Лабораторная | Тема | Статус |
|---|---|---|
| №1 | Lexer / Tokenizer | Реализовано |
| №2 | Parser / AST | Реализовано |
| №3 | Semantic Analyzer | Реализовано |
| №4 | Interpreter / Runtime | Реализовано |
| №5 | Functions / Stack Frames | Реализовано |
| №6 | AST Optimization | Реализовано |
| №7 | Arrays / Data Structures | Реализовано |

---


Проект представляет собой законченный учебный компилятор с полным циклом обработки собственного языка программирования.

