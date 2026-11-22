using static ClosedXML.Parser.ReferenceAxisType;

namespace ClosedXML.Parser.Tests;

public class ReferenceParserTests
{
    [Theory]
    [MemberData(nameof(ParseA1TestCases))]
    public void ParseA1_parses_cell_area_or_rowspan_or_colspan(string text, ReferenceArea expectedReference)
    {
        Assert.Equal(expectedReference, ReferenceParser.ParseA1(text));
    }

    [Fact]
    public void ParseA1_requires_argument()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.ParseA1(null!));
    }

    [Fact]
    public void ParseA1_throws_on_non_references()
    {
        Assert.Throws<ParsingException>(() => ReferenceParser.ParseA1("HELLO"));
    }

    [Theory]
    [MemberData(nameof(ParseA1TestCases))]
    public void TryParseA1_parses_cell_area_or_rowspan_or_colspan(string text, ReferenceArea expectedReference)
    {
        var success = ReferenceParser.TryParseA1(text, out var area);
        Assert.True(success);
        Assert.Equal(expectedReference, area);
    }

    [Fact]
    public void TryParseA1_requires_argument()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.TryParseA1(null!, out _));
    }

    [Fact]
    public void ParseA1_returns_false_on_non_references()
    {
        var success = ReferenceParser.TryParseA1("HELLO", out var area);
        Assert.False(success);
        Assert.Equal(default, area);
    }

    [Theory]
    [MemberData(nameof(ParseSheetA1TestCases))]
    public void TryParseSheetA1_accepts_area_or_rowspan_or_colspan_with_sheet(string text, string expectedSheet, ReferenceArea expectedArea)
    {
        var success = ReferenceParser.TryParseSheetA1(text, out var sheet, out var area);
        Assert.True(success);
        Assert.Equal(expectedSheet, sheet);
        Assert.Equal(expectedArea, area);
    }

    [Fact]
    public void TryParseSheetA1_cant_parse_workbook_index()
    {
        var success = ReferenceParser.TryParseSheetA1("[1]Sheet!A1", out _, out _);
        Assert.False(success);
    }

    [Fact]
    public void TryParseSheetA1_cant_parse_reference_without_sheet()
    {
        var success = ReferenceParser.TryParseSheetA1("A1", out _, out _);
        Assert.False(success);
    }

    [Fact]
    public void TryParseSheetA1_requires_argument()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.TryParseSheetA1(null!, out _, out _));
    }

    [Theory]
    [InlineData("Sheet!Name", "Sheet", "Name")]
    [InlineData("'Hello World'!Name", "Hello World", "Name")]
    [InlineData("' John''s World! '!Name", " John's World! ", "Name")]
    public void TryParseSheetName_parses_sheet_and_name(string text, string expectedSheet, string expectedName)
    {
        var success = ReferenceParser.TryParseSheetName(text, out var sheet, out var name);

        Assert.True(success);
        Assert.Equal(expectedSheet, sheet);
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void TryParseSheetName_requires_text()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.TryParseSheetName(null!, out _, out _));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("some_name")]
    [InlineData("A1")]
    public void TryParseSheetName_cant_parse_pure_name(string text)
    {
        var success = ReferenceParser.TryParseSheetName(text, out _, out _);
        Assert.False(success);
    }

    [Theory]
    [InlineData("Sheet!Name", "Sheet", "Name")]
    [InlineData("'Hello World'!Data", "Hello World", "Data")]
    [InlineData("' John''s World! '!Quarter1", " John's World! ", "Quarter1")]
    public void TryParseName_parses_sheet_and_name(string text, string expectedSheet, string expectedName)
    {
        var success = ReferenceParser.TryParseName(text, out var sheet, out var name);
        Assert.True(success);
        Assert.Equal(expectedSheet, sheet);
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void TryParseName_requires_text()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.TryParseName(null!, out _, out _));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("some_name")]
    public void TryParseName_parses_name(string text)
    {
        var success = ReferenceParser.TryParseName(text, out var sheet, out var name);
        Assert.True(success);
        Assert.Null(sheet);
        Assert.Equal(text, name);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("$BC$1")]
    [InlineData("Sheet!A1")]
    [InlineData("14")]
    [InlineData("\"Text\"")]
    public void TryParseName_cant_parse_anything_but_name(string text)
    {
        var success = ReferenceParser.TryParseName(text, out _, out _);
        Assert.False(success);
    }

    [Theory]
    [MemberData(nameof(ParseA1TestCases))]
    public void TryParseA1_unified_can_parse_local_reference(string text, ReferenceArea expectedReference)
    {
        var success = ReferenceParser.TryParseA1(text, out var sheet, out var reference);
        Assert.True(success);
        Assert.Null(sheet);
        Assert.Equal(expectedReference, reference);
    }

    [Theory]
    [MemberData(nameof(ParseSheetA1TestCases))]
    public void TryParseA1_unified_can_parse_sheet_reference(string text, string expectedSheet, ReferenceArea expectedReference)
    {
        var success = ReferenceParser.TryParseA1(text, out var sheet, out var reference);
        Assert.True(success);
        Assert.Equal(expectedSheet, sheet);
        Assert.Equal(expectedReference, reference);
    }

    [Theory]
    [InlineData("Sheet!Name")]
    [InlineData("Name")]
    [InlineData("1")]
    public void TryParseA1_unified_cant_parse_anything_but_reference(string text)
    {
        var success = ReferenceParser.TryParseA1(text, out _, out _);
        Assert.False(success);
    }

    [Fact]
    public void TryParseA1_unified_requires_argument()
    {
        Assert.Throws<ArgumentNullException>(() => ReferenceParser.TryParseA1(null!, out _, out _));
    }

    #region Span overloads

    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(ParseA1TestCases))]
    public void Span_ParseA1_parses_cell_area_or_rowspan_or_colspan(string text, ReferenceArea expectedReference)
    {
        Assert.Equal(expectedReference, ReferenceParser.ParseA1(text.AsSpan()));
        Assert.True(ReferenceParser.TryParseA1(text.AsSpan(), out var area));
        Assert.Equal(expectedReference, area);
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Span_ParseA1_throws_on_non_references()
    {
        var ex = Assert.Throws<ParsingException>(() => ReferenceParser.ParseA1("HELLO".AsSpan()));
        Assert.Equal("Unable to parse 'HELLO'.", ex.Message);
    }

    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(ParseA1TestCases))]
    public void Span_TryParseA1_unified_gives_empty_sheet_for_local_reference(string text, ReferenceArea expectedReference)
    {
        var success = ReferenceParser.TryParseA1(text.AsSpan(), out ReadOnlySpan<char> sheet, out var reference);
        Assert.True(success);
        Assert.True(sheet.IsEmpty);
        Assert.Equal(expectedReference, reference);
    }

    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(ParseSheetA1TestCases))]
    public void Span_TryParseA1_unified_can_parse_sheet_reference(string text, string expectedSheet, ReferenceArea expectedReference)
    {
        var success = ReferenceParser.TryParseA1(text.AsSpan(), out ReadOnlySpan<char> sheet, out var reference);
        Assert.True(success);
        Assert.Equal(expectedSheet, sheet.ToString());
        Assert.Equal(expectedReference, reference);
    }

    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(ParseSheetA1TestCases))]
    public void Span_TryParseSheetA1_accepts_area_or_rowspan_or_colspan_with_sheet(string text, string expectedSheet, ReferenceArea expectedArea)
    {
        var success = ReferenceParser.TryParseSheetA1(text.AsSpan(), out ReadOnlySpan<char> sheet, out var area);
        Assert.True(success);
        Assert.Equal(expectedSheet, sheet.ToString());
        Assert.Equal(expectedArea, area);
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("[1]Sheet!A1")]
    [InlineData("A1")]
    [InlineData("Sheet!Name")]
    public void Span_TryParseSheetA1_cant_parse_other_texts(string text)
    {
        var success = ReferenceParser.TryParseSheetA1(text.AsSpan(), out ReadOnlySpan<char> sheet, out var area);
        Assert.False(success);
        Assert.True(sheet.IsEmpty);
        Assert.Equal(default, area);
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("Sheet!Name", "Sheet", "Name")]
    [InlineData("'Hello World'!Data", "Hello World", "Data")]
    [InlineData("' John''s World! '!Quarter1", " John's World! ", "Quarter1")]
    public void Span_TryParseName_and_TryParseSheetName_parse_sheet_and_name(string text, string expectedSheet, string expectedName)
    {
        Assert.True(ReferenceParser.TryParseName(text.AsSpan(), out ReadOnlySpan<char> sheet, out var name));
        Assert.Equal(expectedSheet, sheet.ToString());
        Assert.Equal(expectedName, name.ToString());

        Assert.True(ReferenceParser.TryParseSheetName(text.AsSpan(), out sheet, out name));
        Assert.Equal(expectedSheet, sheet.ToString());
        Assert.Equal(expectedName, name.ToString());
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("Name")]
    [InlineData("some_name")]
    public void Span_TryParseName_gives_empty_sheet_and_whole_text_for_name(string text)
    {
        var success = ReferenceParser.TryParseName(text.AsSpan(), out ReadOnlySpan<char> sheet, out var name);
        Assert.True(success);
        Assert.True(sheet.IsEmpty);
        Assert.Equal(text, name.ToString());
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("A1")]
    [InlineData("14")]
    [InlineData("[1]Sheet!Name")]
    public void Span_TryParseName_and_TryParseSheetName_cant_parse_other_texts(string text)
    {
        Assert.False(ReferenceParser.TryParseName(text.AsSpan(), out ReadOnlySpan<char> sheet, out var name));
        Assert.True(sheet.IsEmpty);
        Assert.True(name.IsEmpty);

        Assert.False(ReferenceParser.TryParseSheetName(text.AsSpan(), out sheet, out name));
        Assert.True(sheet.IsEmpty);
        Assert.True(name.IsEmpty);
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Span_outputs_are_slices_of_text_when_sheet_has_no_doubled_tick()
    {
        var text = "'Hello World'!Data".AsSpan();
        Assert.True(ReferenceParser.TryParseSheetName(text, out ReadOnlySpan<char> sheet, out var name));
        Assert.True(text.Overlaps(sheet));
        Assert.True(text.Overlaps(name));

        var escapedText = "'It''s'!A1".AsSpan();
        Assert.True(ReferenceParser.TryParseSheetA1(escapedText, out ReadOnlySpan<char> escapedSheet, out _));
        Assert.Equal("It's", escapedSheet.ToString());
        Assert.False(escapedText.Overlaps(escapedSheet));
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("A1:B2:C3")]
    [InlineData("Sheet!A1:B2:C3")]
    [InlineData("Sheet!A1:B2 ")]
    [InlineData("A1+B1+C1+D1")]
    [InlineData("Sheet!Name+1")]
    public void Text_with_more_tokens_than_reference_is_not_parsed(string text)
    {
        // The lexer stops after a few tokens, so a long text must not match a short token list.
        Assert.False(ReferenceParser.TryParseA1(text.AsSpan(), out ReadOnlySpan<char> _, out _));
        Assert.False(ReferenceParser.TryParseA1(text, out _, out _));
        Assert.False(ReferenceParser.TryParseSheetA1(text, out _, out _));
        Assert.False(ReferenceParser.TryParseName(text, out _, out _));
        Assert.False(ReferenceParser.TryParseSheetName(text, out _, out _));
    }

    #endregion

    public static IEnumerable<object[]> ParseSheetA1TestCases
    {
        get
        {
            yield return new object[]
            {
                "Sheet!$C$2",
                "Sheet",
                new ReferenceArea(new RowCol(Absolute, 2, Absolute, 3, A1)),
            };
            yield return new object[]
            {
                "' ''John''s'' Shop! '!C2",
                " 'John's' Shop! ",
                new ReferenceArea(new RowCol(Relative, 2, Relative, 3, A1)),
            };
            yield return new object[]
            {
                "Sheet!A1:B2",
                "Sheet",
                new ReferenceArea(new RowCol(Relative, 1, Relative, 1, A1), new RowCol(Relative, 2, Relative, 2, A1)),
            };
            yield return new object[]
            {
                "'Some Sheet'!C:D",
                "Some Sheet",
                new ReferenceArea(new RowCol(None, 0, Relative, 3, A1), new RowCol(None, 0, Relative, 4, A1)),
            };
            yield return new object[]
            {
                "'!!WARN'!10:$15",
                "!!WARN",
                new ReferenceArea(new RowCol(Relative, 10, None, 0, A1), new RowCol(Absolute, 15, None, 0, A1)),
            };
        }
    }

    public static IEnumerable<object[]> ParseA1TestCases
    {
        get
        {
            yield return new object[]
            {
                "$C$2",
                new ReferenceArea(new RowCol(Absolute, 2, Absolute, 3, A1)),
            };
            yield return new object[]
            {
                "AB123",
                new ReferenceArea(new RowCol(Relative, 123, Relative, 28, A1)),
            };
            yield return new object[]
            {
                "$C$2:E7",
                new ReferenceArea(new RowCol(Absolute, 2, Absolute, 3, A1), new RowCol(Relative, 7, Relative, 5, A1)),
            };
            yield return new object[]
            {
                "$C:F",
                new ReferenceArea(new RowCol(None, 0, Absolute, 3, A1), new RowCol(None, 0, Relative, 6, A1)),
            };
            yield return new object[]
            {
                "10:$15",
                new ReferenceArea(new RowCol(Relative, 10, None, 0, A1), new RowCol(Absolute, 15, None, 0, A1)),
            };
        }
    }
}
