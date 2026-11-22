using System;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ClosedXML.Parser.Function
{
    public static class ParseFormula
    {
        [Function("parse-formula")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            CancellationToken cancellationToken)
        {
            var refStyle = req.Query["style"] == "R1C1" ? ReferenceStyle.R1C1 : ReferenceStyle.A1;
            string formulaText = req.Query["formula"].ToString();

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new AstNodeConverter(refStyle) }
            };

            try
            {
                var nodes = refStyle == ReferenceStyle.A1
                    ? FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formulaText, new Ctx(), new F())
                    : FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaR1C1(formulaText, new Ctx(), new F());

                // The default maximum JSON depth is 64. A chain of binary operators makes a deep AST, and each AST
                // level uses two JSON levels: the node object and its children array. The root object uses one level.
                serializerOptions.MaxDepth = 2 * GetDepth(nodes) + 1;
                return new JsonResult(new
                {
                    formula = formulaText,
                    style = refStyle.ToString(),
                    ast = nodes
                }, serializerOptions);
            }
            catch (ParsingException e)
            {
                return new JsonResult(new
                {
                    formula = formulaText,
                    style = refStyle.ToString(),
                    error = e.Message
                }, serializerOptions)
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity
                };
            }
        }

        private static int GetDepth(AstNode node)
        {
            var childDepth = 0;
            foreach (var child in node.Children)
                childDepth = Math.Max(childDepth, GetDepth(child));

            return childDepth + 1;
        }
    }
}
