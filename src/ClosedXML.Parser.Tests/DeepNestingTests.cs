using System.Runtime.ExceptionServices;

namespace ClosedXML.Parser.Tests;

/// <summary>
/// The parser is a recursive descent parser. Each nesting level of a formula uses stack. A stack
/// overflow stops the process, and the caller cannot catch it. Thus the parser must throw
/// <see cref="ParsingException"/> before the stack overflows.
/// </summary>
public class DeepNestingTests
{
    // Excel limits the text of a cell formula to 8192 chars. An .xlsx file from any source can
    // contain a formula of that length.
    private const int MaxFormulaLength = 8192;

    // Excel limits a formula to 64 nesting levels of functions.
    private const int ExcelNestingLimit = 64;

    // The parser has no length limit. This number of nesting levels is too deep for the small
    // stack on each JIT tier. The operating system can give a thread a larger stack than the
    // requested size, so the number is much larger than necessary.
    private const int DeepNesting = 100_000;

    // On this small stack, a formula of the maximum length can overflow the stack.
    private const int SmallStackSize = 256 * 1024;

    // 1 MB is the default stack size of a thread on Windows.
    private const int DefaultStackSize = 1024 * 1024;

    // Each row is a recursion path of the parser: a prefix and a suffix around an operand.
    public static TheoryData<string, string, string> NestingPaths => new()
    {
        { "(", "A1", ")" }, // Parentheses around a value expression
        { "SUM(", "1", ")" }, // Function arguments
        { "IF(TRUE,", "1", ",0)" }, // Arguments of a function that can give a reference
        { "INDEX(", "A1", ",1)" }, // Arguments of a function in a reference expression
        { "-", "1", "" }, // Unary minus
        { "+", "1", "" }, // Unary plus
        { "@ ", "A1", "" }, // Implicit intersection. Without the spaces, the lexer time of a long text is quadratic.
        { "@(", "A1", ")" }, // Implicit intersection of a reference in parentheses
        { "A1:(", "A1", ")" }, // Range operand in parentheses
        { "A1 (", "A1", ")" }, // Intersection operand in parentheses
        { "A1,(", "A1", ")" }, // Union operand in parentheses
        { "(-", "{1,2}", ")" }, // Array constant in nested parentheses and unary minus
    };

    [Theory]
    [MemberData(nameof(NestingPaths))]
    public void Parsers_throw_parsing_exception_for_deep_nesting(string prefix, string operand, string suffix)
    {
        var formula = Nest(prefix, operand, suffix, DeepNesting);

        Assert.Throws<ParsingException>(() => ParseOnThread(formula, true, SmallStackSize));
        Assert.Throws<ParsingException>(() => ParseOnThread(formula, false, SmallStackSize));
    }

    [Fact]
    public void Parsers_throw_parsing_exception_for_text_of_only_minus_signs()
    {
        // Each minus sign is a nesting level, and the text has no operand.
        var formula = new string('-', MaxFormulaLength);

        Assert.Throws<ParsingException>(() => ParseOnThread(formula, true, SmallStackSize));
        Assert.Throws<ParsingException>(() => ParseOnThread(formula, false, SmallStackSize));
    }

    [Theory]
    [MemberData(nameof(NestingPaths))]
    public void Can_parse_nesting_at_excel_limit(string prefix, string operand, string suffix)
    {
        var formula = Nest(prefix, operand, suffix, ExcelNestingLimit);

        Assert.NotNull(ParseOnThread(formula, true, DefaultStackSize));
        Assert.NotNull(ParseOnThread(formula, false, DefaultStackSize));
    }

    private static string Nest(string prefix, string operand, string suffix, int depth)
    {
        return string.Concat(Enumerable.Repeat(prefix, depth)) + operand + string.Concat(Enumerable.Repeat(suffix, depth));
    }

    private static AstNode ParseOnThread(string formula, bool a1Mode, int stackSize)
    {
        AstNode? node = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                node = a1Mode
                    ? FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F())
                    : FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaR1C1(formula, new Ctx(), new F());
            }
            catch (Exception e)
            {
                error = e;
            }
        }, stackSize);
        thread.Start();
        thread.Join();

        if (error is not null)
            ExceptionDispatchInfo.Throw(error);

        return node!;
    }
}
