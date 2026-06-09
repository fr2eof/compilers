namespace Lexer
{
    public abstract class LlmrInstruction
    {
    }

    public class LabelInstruction : LlmrInstruction
    {
        public string Name { get; }
        public LabelInstruction(string name) { Name = name; }
        public override string ToString() => $"{Name}:";
    }

    public class AssignmentInstruction : LlmrInstruction
    {
        public string Target { get; }
        public string Value { get; }
        public AssignmentInstruction(string target, string value) { Target = target; Value = value; }
        public override string ToString() => $"  {Target} = {Value}";
    }

    public class BinaryInstruction : LlmrInstruction
    {
        public string Target { get; }
        public string Left { get; }
        public string Op { get; }
        public string Right { get; }
        public BinaryInstruction(string target, string left, string op, string right)
        {
            Target = target; Left = left; Op = op; Right = right;
        }
        public override string ToString() => $"  {Target} = {Left} {Op} {Right}";
    }

    public class CallInstruction : LlmrInstruction
    {
        public string Target { get; }
        public string Function { get; }
        public List<string> Arguments { get; }
        public CallInstruction(string target, string function, List<string> arguments)
        {
            Target = target; Function = function; Arguments = arguments;
        }
        public override string ToString() => $"  {Target} = call {Function}({string.Join(", ", Arguments)})";
    }

    public class ReturnInstruction : LlmrInstruction
    {
        public string Value { get; }
        public ReturnInstruction(string value) { Value = value; }
        public override string ToString() => $"  return {Value}";
    }

    public class JumpIfFalseInstruction : LlmrInstruction
    {
        public string Condition { get; }
        public string Label { get; }
        public JumpIfFalseInstruction(string condition, string label) { Condition = condition; Label = label; }
        public override string ToString() => $"  jump_if_false {Condition} goto {Label}";
    }

    public class JumpInstruction : LlmrInstruction
    {
        public string Label { get; }
        public JumpInstruction(string label) { Label = label; }
        public override string ToString() => $"  jump {Label}";
    }

    public class PrintInstruction : LlmrInstruction
    {
        public string Value { get; }
        public PrintInstruction(string value) { Value = value; }
        public override string ToString() => $"  print {Value}";
    }
}
