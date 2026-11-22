using System.Globalization;
using static ClosedXML.Parser.ReferenceAxisType;

namespace ClosedXML.Parser.Tests;

public class SpanFormattableTests
{
    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(RowColCases))]
    public void RowCol_is_formatted_in_its_own_style(RowCol rowCol, string expected)
    {
        Assert.Equal(expected, rowCol.ToString());
        Assert.Equal(expected, rowCol.ToString(null, null));
        Assert.Equal(expected, rowCol.ToString(string.Empty, CultureInfo.InvariantCulture));
        Assert.Equal(expected, $"{rowCol}");
        AssertTryFormat(rowCol, expected);
    }

    [Theory]
    [Trait("Issue", "22")]
    [MemberData(nameof(ReferenceAreaCases))]
    public void ReferenceArea_is_formatted_in_its_own_style(ReferenceArea area, string expected)
    {
        Assert.Equal(expected, area.ToString());
        Assert.Equal(expected, area.ToString(null, null));
        Assert.Equal(expected, area.ToString(string.Empty, CultureInfo.InvariantCulture));
        Assert.Equal(expected, $"{area}");
        AssertTryFormat(area, expected);
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Format_must_be_empty()
    {
        var rowCol = new RowCol(Relative, 1, Relative, 1, A1);
        var area = new ReferenceArea(rowCol);

        Assert.Throws<FormatException>(() => rowCol.ToString("G", null));
        Assert.Throws<FormatException>(() => area.ToString("G", null));
        Assert.Throws<FormatException>(() => rowCol.TryFormat(new char[10], out _, "G"));
        Assert.Throws<FormatException>(() => area.TryFormat(new char[10], out _, "G"));
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Longest_reference_fits_in_buffer()
    {
        // The int.MinValue gives the longest number. The parser does not make such values, but the
        // public constructor accepts them.
        var rowCol = new RowCol(Relative, int.MinValue, Relative, int.MinValue, R1C1);
        var area = new ReferenceArea(rowCol, new RowCol(Relative, int.MinValue, Relative, int.MaxValue, R1C1));
        var a1 = new RowCol(Absolute, int.MinValue, Absolute, int.MaxValue, A1);

        Assert.Equal("R[-2147483648]C[-2147483648]", rowCol.GetDisplayStringR1C1());
        Assert.Equal("R[-2147483648]C[-2147483648]:R[-2147483648]C[2147483647]", area.GetDisplayStringR1C1());
        Assert.Equal("$FXSHRXW$-2147483648", a1.GetDisplayStringA1());
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Negative_number_does_not_use_minus_sign_of_culture()
    {
        // Some cultures use other minus signs (e.g. U+2212). A formula needs the ASCII minus.
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var area = new ReferenceArea(new RowCol(Relative, -1, Relative, -2, R1C1));

            Assert.Equal("R[-1]C[-2]", area.GetDisplayStringR1C1());
            Assert.Equal("R[-1]C[-2]", FormulaConverter.ToR1C1("A1", 2, 3));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    public static IEnumerable<object[]> RowColCases
    {
        get
        {
            yield return new object[] { new RowCol(Absolute, 7, Absolute, 16384, A1), "$XFD$7" };
            yield return new object[] { new RowCol(Relative, 7, None, 0, A1), "7" };
            yield return new object[] { new RowCol(Relative, -1, Relative, 0, R1C1), "R[-1]C" };
            yield return new object[] { new RowCol(Absolute, 5, None, 0, R1C1), "R5" };
        }
    }

    public static IEnumerable<object[]> ReferenceAreaCases
    {
        get
        {
            yield return new object[] { new ReferenceArea(new RowCol(Relative, 1, Relative, 1, A1)), "A1" };
            yield return new object[] { new ReferenceArea(new RowCol(Relative, 1, Relative, 1, A1), new RowCol(Absolute, 5, Absolute, 3, A1)), "A1:$C$5" };
            yield return new object[] { new ReferenceArea(new RowCol(Relative, 6, None, 0, A1)), "6:6" };
            yield return new object[] { new ReferenceArea(new RowCol(Relative, 2, Relative, -3, R1C1)), "R[2]C[-3]" };
            yield return new object[] { new ReferenceArea(new RowCol(Absolute, 1, None, 0, R1C1), new RowCol(Absolute, 4, None, 0, R1C1)), "R1:R4" };
        }
    }

    private static void AssertTryFormat<T>(T value, string expected) where T : ISpanFormattable
    {
        // A destination with the exact length is sufficient.
        var destination = new char[expected.Length];
        Assert.True(value.TryFormat(destination, out var charsWritten, default, null));
        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(destination));

        // A shorter destination is not sufficient. TryFormat writes nothing into it.
        var shortDestination = new char[expected.Length - 1];
        Assert.False(value.TryFormat(shortDestination, out charsWritten, default, null));
        Assert.Equal(0, charsWritten);
        Assert.All(shortDestination, c => Assert.Equal('\0', c));
    }
}
