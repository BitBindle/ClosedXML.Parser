using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ClosedXML.Parser.Function
{
    public static class ParseFormula
    {
        private const int DefaultMaxDepth = 64;

        /// <summary>
        /// The serializer options for each reference style and maximum depth. Options cache the type metadata, so
        /// a new instance for each request is slow. The maximum depth is a power of two, thus there are only a
        /// few instances.
        /// </summary>
        private static readonly ConcurrentDictionary<(ReferenceStyle Style, int MaxDepth), JsonSerializerOptions> SerializerOptions = new();

        [Function("parse-formula")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            CancellationToken cancellationToken)
        {
            var refStyle = req.Query["style"] == "R1C1" ? ReferenceStyle.R1C1 : ReferenceStyle.A1;
            string formulaText = req.Query["formula"].ToString();

            try
            {
                var nodes = refStyle == ReferenceStyle.A1
                    ? FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formulaText, new Ctx(), new F())
                    : FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaR1C1(formulaText, new Ctx(), new F());

                // The default maximum JSON depth is 64. A chain of binary operators makes a deep AST, and each AST
                // level uses two JSON levels: the node object and its children array. The root object uses one level.
                return new JsonResult(new
                {
                    formula = formulaText,
                    style = refStyle.ToString(),
                    ast = nodes
                }, GetSerializerOptions(refStyle, 2 * GetDepth(nodes) + 1));
            }
            catch (ParsingException e)
            {
                return new JsonResult(new
                {
                    formula = formulaText,
                    style = refStyle.ToString(),
                    error = e.Message
                }, GetSerializerOptions(refStyle, DefaultMaxDepth))
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity
                };
            }
        }

        private static JsonSerializerOptions GetSerializerOptions(ReferenceStyle refStyle, int minMaxDepth)
        {
            var maxDepth = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(minMaxDepth, DefaultMaxDepth));
            return SerializerOptions.GetOrAdd((refStyle, maxDepth), static key => new JsonSerializerOptions
            {
                WriteIndented = true,
                MaxDepth = key.MaxDepth,
                Converters = { new AstNodeConverter(key.Style) }
            });
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
