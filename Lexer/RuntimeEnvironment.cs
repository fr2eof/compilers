namespace Lexer
{
    public sealed class RuntimeEnvironment
    {
        private readonly Stack<Dictionary<string, RuntimeValue>> scopes = new();

        public RuntimeEnvironment()
        {
            scopes.Push(new Dictionary<string, RuntimeValue>());
        }

        public void PushScope()
        {
            scopes.Push(new Dictionary<string, RuntimeValue>());
        }

        public void PopScope()
        {
            if (scopes.Count == 1)
            {
                throw new InvalidOperationException("[Runtime Error] Cannot exit the global scope.");
            }

            scopes.Pop();
        }

        public void Declare(string name, object? value = null, bool initialized = false)
        {
            Dictionary<string, RuntimeValue> currentScope = scopes.Peek();

            if (currentScope.ContainsKey(name))
            {
                throw new InvalidOperationException($"[Runtime Error] Variable '{name}' is already declared in this scope.");
            }

            currentScope[name] = new RuntimeValue(value, initialized);
        }

        public object? Get(string name)
        {
            foreach (Dictionary<string, RuntimeValue> scope in scopes)
            {
                if (scope.TryGetValue(name, out RuntimeValue value))
                {
                    if (!value.Initialized)
                    {
                        throw new InvalidOperationException($"[Runtime Error] Variable '{name}' is not initialized.");
                    }

                    return value.Value;
                }
            }

            throw new InvalidOperationException($"[Runtime Error] Variable '{name}' is not declared.");
        }

        public void Assign(string name, object? value)
        {
            foreach (Dictionary<string, RuntimeValue> scope in scopes)
            {
                if (scope.ContainsKey(name))
                {
                    scope[name] = new RuntimeValue(value, true);
                    return;
                }
            }

            throw new InvalidOperationException($"[Runtime Error] Variable '{name}' is not declared.");
        }

        private readonly record struct RuntimeValue(object? Value, bool Initialized);
    }
}