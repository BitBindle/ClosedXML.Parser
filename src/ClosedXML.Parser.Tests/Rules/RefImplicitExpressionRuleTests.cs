namespace ClosedXML.Parser.Tests.Rules;

public class RefImplicitExpressionRuleTests
{
    [Fact]
    public void Implicit_intersection_operator_has_lower_priority_than_range()
    {
        var expectedNode =
            new UnaryNode(
                UnaryOperation.ImplicitIntersection,
                new BinaryNode(
                    BinaryOperation.Range,
                    new ReferenceNode(new ReferenceArea(new RowCol(1, 1, A1), new RowCol(2, 1, A1))),
                    new ReferenceNode(new ReferenceArea(3, 1, A1))));
        AssertFormula.SingleNodeParsed("@A1:A2:A3", expectedNode);
    }

    [Theory]
    [Trait("Issue", "2")]
    [InlineData("(A1)@")]
    [InlineData("((A1))@")]
    [InlineData("SUM((A1)@)")]
    [InlineData("(A1)@B2")]
    [InlineData("(A1:B2)@,C3")]
    public void Implicit_intersection_operator_after_reference_in_braces_is_error(string formula)
    {
        // The implicit intersection operator is a prefix operator. Excel does not accept it after an operand.
        Assert.Throws<ParsingException>(() => FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F()));
    }

    [Fact]
    [Trait("Issue", "2")]
    public void Implicit_intersection_operator_before_reference_in_braces_is_parsed()
    {
        var intersectA1 =
            new UnaryNode(
                UnaryOperation.ImplicitIntersection,
                new ReferenceNode(new ReferenceArea(1, 1, A1)));
        AssertFormula.SingleNodeParsed("@(A1)", intersectA1);
        AssertFormula.SingleNodeParsed("(@A1)", intersectA1);

        var unionWithIntersectB2 =
            new BinaryNode(
                BinaryOperation.Union,
                new ReferenceNode(new ReferenceArea(1, 1, A1)),
                new UnaryNode(
                    UnaryOperation.ImplicitIntersection,
                    new ReferenceNode(new ReferenceArea(2, 2, A1))));
        AssertFormula.SingleNodeParsed("(A1),@B2", unionWithIntersectB2);
    }
}