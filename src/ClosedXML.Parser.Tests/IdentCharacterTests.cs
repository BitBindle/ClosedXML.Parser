namespace ClosedXML.Parser.Tests;

/// <summary>
/// The chars that the parser accepts in names and in unquoted sheet names. The rules are the
/// <c>NAME</c> and <c>SINGLE_SHEET_PREFIX</c> tokens of the lexer.
/// </summary>
public class IdentCharacterTests
{
    [Theory]
    [Trait("Issue", "16")]
    [InlineData("name0")]
    [InlineData("n.a.m.e")]
    [InlineData("name?")]
    [InlineData("na?me")]
    [InlineData("\\name")]
    [InlineData("name\\")]
    [InlineData("name_")]
    [InlineData("_xlfn.ACOT")]
    [InlineData("\u00A0name")] // NO-BREAK SPACE
    [InlineData("na\u00A0me")]
    [InlineData("na\u2028me")] // LINE SEPARATOR
    [InlineData("\u0080name")] // A control char
    [InlineData("\u20AC")] // EURO SIGN
    [InlineData("\u00BDname")] // VULGAR FRACTION ONE HALF
    [InlineData("\u0660")] // ARABIC-INDIC DIGIT ZERO
    [InlineData("\u0301name")] // COMBINING ACUTE ACCENT
    [InlineData("\uE000")] // A private use char
    [InlineData("\U0001F600")] // GRINNING FACE
    [InlineData("name\U0001F600")]
    [InlineData("A0")] // A text that reads almost as a cell
    [InlineData("A01")]
    [InlineData("XFE1")]
    [InlineData("A1048577")]
    public void Parser_reads_name(string formula)
    {
        Assert.IsType<NameNode>(Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("1x!A1")]
    [InlineData("1!A1")]
    [InlineData("1.5!A1")]
    [InlineData("$x!A1")]
    [InlineData(".x!A1")]
    [InlineData("\"x\"!A1")]
    [InlineData("x\"!A1")]
    [InlineData("a;b!A1")]
    [InlineData("a@b!A1")]
    [InlineData("a|b!A1")]
    [InlineData("a~b!A1")]
    [InlineData("a`b!A1")]
    [InlineData("a_b!A1")]
    [InlineData("a\u007Fb!A1")] // DELETE
    [InlineData("a\tb!A1")]
    [InlineData("a\nb!A1")]
    [InlineData("a\rb!A1")]
    [InlineData("\t!A1")]
    [InlineData("\u00A0!A1")]
    [InlineData("\u20AC!A1")]
    [InlineData("\uFFFD!A1")] // The last valid char in the basic multilingual plane
    [InlineData("\U0001F600!A1")]
    [InlineData("TRUE!A1")]
    [InlineData("Sheet! A1")] // The sheet prefix of the lexer contains the spaces after the `!`
    [InlineData("\nSheet!A1")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!A1")] // The parser does not check the length
    public void Parser_reads_unquoted_sheet(string formula)
    {
        Assert.IsType<SheetReferenceNode>(Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("1+\nSheet!A1", "1+Sheet!A1")] // The lexer adds the LF to the operator, not to the sheet name
    [InlineData("1+\rSheet!A1", "1+Sheet!A1")]
    [InlineData("1- \r\nSheet!A1", "1-Sheet!A1")]
    [InlineData("1*\n\n1x!A1", "1*1x!A1")]
    [InlineData("(\nSheet!A1)", "(Sheet!A1)")]
    public void Parser_reads_sheet_after_operator_without_whitespace(string formula, string sameFormula)
    {
        Assert.Equal(Parse(sameFormula), Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a;b!x")]
    [InlineData("x!a.b")]
    [InlineData("x!\u00A0a")]
    [InlineData("x!\U0001F600")]
    public void Parser_reads_sheet_name(string formula)
    {
        Assert.IsType<SheetNameNode>(Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a:c;d!A1")] // The last sheet is a sheet prefix
    [InlineData("a:1!A1")]
    [InlineData("a.b:c$d!A1")]
    [InlineData("_a:b!A1")]
    [InlineData("\U0001F600:b!A1")]
    [InlineData("a:\U0001F600!A1")]
    public void Parser_reads_3d_reference(string formula)
    {
        Assert.IsType<Reference3DNode>(Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a\\b!A1")] // An unquoted sheet name can not contain a backslash
    [InlineData("a?b!A1")]
    [InlineData("\uFFFE!A1")] // A noncharacter
    [InlineData("a;b:c!A1")] // The first sheet of a 3D reference must be a name
    [InlineData("1a:b!A1")]
    [InlineData("a$b:c!A1")]
    [InlineData("0name")]
    [InlineData("name(")]
    [InlineData("name)")]
    [InlineData("name,")]
    [InlineData("?name")]
    [InlineData(".name")]
    public void Parser_rejects_formula(string formula)
    {
        Assert.Throws<ParsingException>(() => Parse(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("\\a:b!A1")]
    [InlineData("a?:b!A1")]
    public void Parser_reads_name_that_is_not_a_sheet_as_operand_of_range(string formula)
    {
        // A sheet name can not contain these chars, so the first operand is a name.
        var node = Parse(formula);

        var range = Assert.IsType<BinaryNode>(node);
        Assert.Equal(BinaryOperation.Range, range.Operation);
        Assert.IsType<NameNode>(range.Children[0]);
        Assert.IsType<SheetReferenceNode>(range.Children[1]);
    }

    [Fact]
    [Trait("Issue", "16")]
    public void Unquoted_sheet_name_from_writer_is_valid_for_parser()
    {
        // The writer can quote more than the parser needs, but each sheet name that the
        // writer does not quote must be valid for the parser.
        for (var codepoint = 1; codepoint <= 0x10FFFF; ++codepoint)
        {
            if (codepoint is >= 0xD800 and <= 0xDFFF)
                continue;

            // An XML file can not contain a control char, except tab, LF and CR. A quoted sheet
            // name can not contain it either.
            if (codepoint < 0x20 && codepoint is not '\t' and not '\n' and not '\r')
                continue;

            var c = char.ConvertFromUtf32(codepoint);
            foreach (var sheet in new[] { c, "a" + c, c + "a" })
            {
                // Excel does not accept a sheet name that starts with an apostrophe.
                if (sheet[0] == '\'')
                    continue;

                if (NameUtils.IsSheetNameValid(sheet) && !NameUtils.ShouldQuote(sheet))
                    Assert.True(NameUtils.IsUnquotedSheetName(sheet), $"Sheet with U+{codepoint:X4} is not valid without quotes.");
            }
        }
    }

    [Fact]
    [Trait("Issue", "16")]
    public void IsUnquotedSheetName_checks_sheet_name()
    {
        // xUnit changes an unpaired surrogate in InlineData, so the test has the values in code.
        Assert.False(NameUtils.IsUnquotedSheetName(""));
        Assert.True(NameUtils.IsUnquotedSheetName("a"));
        Assert.False(NameUtils.IsUnquotedSheetName("a b"));
        Assert.False(NameUtils.IsUnquotedSheetName("a\uD800")); // Unpaired high surrogate
        Assert.False(NameUtils.IsUnquotedSheetName("a\uD800b"));
        Assert.False(NameUtils.IsUnquotedSheetName("\uDC00")); // Unpaired low surrogate
        Assert.True(NameUtils.IsUnquotedSheetName("\uD83D\uDE00"));
    }

    private static AstNode Parse(string formula)
    {
        return FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F());
    }
}
