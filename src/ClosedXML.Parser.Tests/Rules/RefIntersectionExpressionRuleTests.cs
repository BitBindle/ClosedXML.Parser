namespace ClosedXML.Parser.Tests.Rules;

public class RefIntersectionExpressionRuleTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void Has_one_or_more_elements_separated_by_space(string formula, AstNode expectedNode)
    {
        AssertFormula.SingleNodeParsed(formula, expectedNode);
    }

    [Fact]
    public void Intersection_operator_has_lower_priority_than_implicit_intersection()
    {
        var expectedNode =
            new UnaryNode(
                UnaryOperation.ImplicitIntersection,
                new BinaryNode(
                    BinaryOperation.Intersection,
                    new ReferenceNode(new ReferenceArea(new RowCol(1,1, A1), new RowCol(10,1, A1))),
                    new ReferenceNode(new ReferenceArea(5, 1, A1))));
        AssertFormula.SingleNodeParsed("@A1:A10 A5", expectedNode);
    }

    [Theory]
    [MemberData(nameof(SpaceInOtherTokenTestCases))]
    public void Space_in_brace_or_error_token_is_intersection_operator(string formula, AstNode expectedNode)
    {
        // The tokens of braces and of the spill operator include the whitespaces around them.
        // The space of the operator is then not a separate SPACE token.
        AssertFormula.SingleNodeParsed(formula, expectedNode);
    }

    [Theory]
    [InlineData("A1 (B1)")]
    [InlineData("A1 #REF!")]
    public void Line_break_is_not_intersection_operator(string formulaWithSpace)
    {
        // The lexer accepts only a space as the operator, not a line break.
        AssertFormula.CheckParsingErrorContains(formulaWithSpace.Replace(' ', '\n'), "wasn't parsed correctly");
    }

    public static IEnumerable<object[]> SpaceInOtherTokenTestCases
    {
        get
        {
            var a1C3 = new ReferenceNode(new ReferenceArea(new RowCol(1, 1, A1), new RowCol(3, 3, A1)));
            var b2D4 = new ReferenceNode(new ReferenceArea(new RowCol(2, 2, A1), new RowCol(4, 4, A1)));
            var a1 = new ReferenceNode(new ReferenceArea(1, 1, A1));
            var b1 = new ReferenceNode(new ReferenceArea(1, 2, A1));
            var refError = new ValueNode("Error", "#REF!");

            // CLOSE_BRACE takes the space.
            yield return new object[]
            {
                "(A1:C3) (B2:D4)",
                new BinaryNode(BinaryOperation.Intersection, a1C3, b2D4)
            };

            // OPEN_BRACE takes the space.
            yield return new object[]
            {
                "A1:C3 (A1,B1)",
                new BinaryNode(BinaryOperation.Intersection, a1C3, new BinaryNode(BinaryOperation.Union, a1, b1))
            };

            // A union in braces as the left operand.
            yield return new object[]
            {
                "(A1,B1) B2:D4",
                new BinaryNode(BinaryOperation.Intersection, new BinaryNode(BinaryOperation.Union, a1, b1), b2D4)
            };

            // A function call as the left operand.
            yield return new object[]
            {
                "IF(TRUE,A1:C3) B2:D4",
                new BinaryNode(BinaryOperation.Intersection,
                    new FunctionNode("IF") { Children = new AstNode[] { new ValueNode(true), a1C3 } },
                    b2D4)
            };

            // A function call as each operand.
            yield return new object[]
            {
                "INDEX(A1:C3,1) INDEX(B2:D4,1)",
                new BinaryNode(BinaryOperation.Intersection,
                    new FunctionNode("INDEX") { Children = new AstNode[] { a1C3, new ValueNode(1) } },
                    new FunctionNode("INDEX") { Children = new AstNode[] { b2D4, new ValueNode(1) } })
            };

            // Three operands, where the braces take both spaces.
            yield return new object[]
            {
                "A1 (B1) A1:C3",
                new BinaryNode(BinaryOperation.Intersection,
                    new BinaryNode(BinaryOperation.Intersection, a1, b1),
                    a1C3)
            };

            // INTRA_TABLE_REFERENCE takes the space.
            yield return new object[]
            {
                "Table[Column] A1",
                new BinaryNode(BinaryOperation.Intersection,
                    new StructureReferenceNode("Table", StructuredReferenceArea.None, "Column", "Column"),
                    a1)
            };

            // The lexer splits ` #REF!` into a SPILL token and a SINGLE_SHEET_PREFIX token.
            yield return new object[]
            {
                "A1:C3 #REF!",
                new BinaryNode(BinaryOperation.Intersection, a1C3, refError)
            };

            yield return new object[]
            {
                "A1 #ref! B1",
                new BinaryNode(BinaryOperation.Intersection,
                    new BinaryNode(BinaryOperation.Intersection, a1, refError),
                    b1)
            };

            // A reference of a deleted sheet is a part of the error.
            yield return new object[]
            {
                "A1 #REF!A1:C3",
                new BinaryNode(BinaryOperation.Intersection, a1, refError)
            };

            // A spill operator without a space before it stays a spill operator.
            yield return new object[]
            {
                "A1# B1",
                new BinaryNode(BinaryOperation.Intersection, new UnaryNode(UnaryOperation.SpillRange, a1), b1)
            };
        }
    }

    public static IEnumerable<object[]> TestCases
    {
        get
        {
            // ref_intersection_expression : ref_range_expression
            yield return new object[]
            {
                "A1",
                new ReferenceNode(new ReferenceArea(1, 1, A1))
            };

            // ref_intersection_expression : ref_range_expression SPACE ref_range_expression
            yield return new object[]
            {
                "A1 A2",
                new BinaryNode(BinaryOperation.Intersection,
                    new ReferenceNode(new ReferenceArea(1, 1, A1)),
                    new ReferenceNode(new ReferenceArea(2, 1, A1)))
            };

            // ref_intersection_expression : ref_range_expression SPACE ref_range_expression
            yield return new object[]
            {
                " A1   A2   A3  ",
                new BinaryNode(BinaryOperation.Intersection,
                    new BinaryNode(BinaryOperation.Intersection,
                        new ReferenceNode(new ReferenceArea(1, 1, A1)),
                        new ReferenceNode(new ReferenceArea(2, 1, A1))),
                    new ReferenceNode(new ReferenceArea(3, 1, A1)))
            };
        }
    }
}