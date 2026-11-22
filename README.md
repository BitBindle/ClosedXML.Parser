# ClosedXML.Parser

ClosedXML.Parser is a .NET library that parses Excel formulas into an abstract syntax tree (AST). ClosedXML uses it to evaluate formulas and to change references in formulas.

This repository is a fork of [ClosedXML.Parser](https://github.com/ClosedXML/ClosedXML.Parser) by Jan Havlíček. The fork has its own releases, issues and changes. The license is MIT (see [LICENSE.txt](https://github.com/BitBindle/closedxml.parser/blob/develop/LICENSE.txt)).

## Package

The release workflow publishes the `ClosedXML.Parser` package to the GitHub Packages NuGet feed of BitBindle. The package targets `net10.0`.

GitHub Packages needs authentication, also for a public package. Use a token with the `read:packages` scope.

The package ID is the same as the ID of the upstream package on nuget.org. The release workflow does not publish a version that nuget.org already has. Use package source mapping, so that NuGet gets `ClosedXML.Parser` only from the fork feed.

This example `nuget.config` reads the user name and the token from the `GITHUB_USER` and `GITHUB_TOKEN` environment variables:

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github-bitbindle" value="https://nuget.pkg.github.com/BitBindle/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github-bitbindle>
      <add key="Username" value="%GITHUB_USER%" />
      <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
    </github-bitbindle>
  </packageSourceCredentials>
  <packageSourceMapping>
    <packageSource key="github-bitbindle">
      <package pattern="ClosedXML.Parser" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## How to use

1. Implement the `IAstFactory<TScalarValue, TNode, TContext>` interface. The parser calls the factory for each part of the formula, and the factory creates your nodes.
2. Call a parse method:
   * `FormulaParser<TScalarValue, TNode, TContext>.CellFormulaA1("SUM(A1, 2)", context, factory)`
   * `FormulaParser<TScalarValue, TNode, TContext>.CellFormulaR1C1("SUM(R1C1, 2)", context, factory)`

The library also has these helpers:

* `FormulaConverter` changes a formula from A1 to R1C1 and from R1C1 to A1. Its `ModifyA1` method changes a formula with a factory that you give, for example a subclass of `RefModVisitor`.
* `ReferenceParser` parses a reference, for example `Sheet1!A1:B2`.
* `NameUtils` finds when a sheet name needs quotes in a formula.

The `ClosedXML.Parser.Ast` project has a simple AST and a factory. The tests use it. It shows how to implement `IAstFactory`.

## Goals

* __Performance__: ClosedXML must parse formulas fast and with few allocations.
* __Evaluation__: The parser creates an abstract syntax tree, not a concrete syntax tree. The goal is the evaluation of formulas.
* __Multi-use__: Formulas are mostly in cells, but other parts of a workbook have formulas with different grammar rules, for example sparklines and data validation.
* __A1 and R1C1 notation__: The parser reads formulas in A1 and R1C1 notation. For example, `SUM(R5)` is the sum of cell `R5` in A1 notation, but the sum of row 5 in R1C1 notation.

## Design

The official source of the grammar is [MS-XLSX](https://learn.microsoft.com/en-us/openspecs/office_standards/ms-xlsx/2c5dee00-eff2-4b22-92b6-0738acd4475e), chapter 2.2.2 Formulas. A copy is in [MS-XLSX-parser-grammar.abnf](https://github.com/BitBindle/closedxml.parser/blob/develop/MS-XLSX-parser-grammar.abnf). A parser generator can not use this grammar directly: it has ambiguities, and its rules do not show the operator precedence.

The ANTLR4 grammar in `src/ClosedXML.ANTLR` is the source of truth for the lexer and the parser:

* __Lexer__: Rolex generates a DFA lexer from the ANTLR lexer grammar (see [Generate the lexer](#generate-the-lexer)). It is approximately two times faster than the ANTLR lexer.
* __Parser__: `FormulaParser` is a recursive descent parser. It follows the rules of the ANTLR parser grammar, but it does not use ANTLR at runtime.

### Performance

The ENRON data set has 946320 formulas. The recursive descent parser with the DFA lexer parses them in approximately 1.8 seconds in Release mode. That is approximately 2 μs for each formula.

### Limitations

The primary goal is to parse the formulas in a file, not the formulas that a user types. The formula in the file is not always the same as the formula that Excel shows. For example:

* The `IFS` function is a future function. The file has `_xlfn.IFS`, but the user sees `IFS`.
* In a structured reference, the user sees `@` for the current row. The file has the `[#This Row]` specifier.

Thus:

* An external reference must use the index of an external file, for example `[5]`.
* Other formula grammars, for example `[1]!'Some name in external wb'`, are out of scope.

## Development

[AGENTS.md](https://github.com/BitBindle/closedxml.parser/blob/develop/AGENTS.md) gives the rules for each change, and the commands to build, test and check the code. The short form:

```bash
dotnet build ClosedXML.Parser.slnx --configuration Release
```

```bash
dotnet test src/ClosedXML.Parser.Tests/ClosedXML.Parser.Tests.csproj --configuration Release --no-build
```

The base branch is `develop`. The CI workflow builds and tests each pull request to `develop`. A person starts the release workflow on `develop`.

### Testing strategy

* Some tokens have data for a node, for example `A1_CELL` `C5` gives row 5 and column 3. The `Lexers` folder has test classes for most of these tokens, for example `A1CellTokenTests.cs`.
* The `Rules` folder has test classes for the parser rules, for example `CellReferenceRuleTests.cs`. Each test class has combinations of the rule, and it compares the result with the AST nodes.
* `DataSetTests.cs` has the data set tests. Each test parses each formula in `data/<set>/formulas.csv`. The parse must fail for the formulas in the `known-fails.csv` and `invalid-external-cell-reference.csv` files of the set, if the set has them. The parse must not fail for the other formulas. The tests do not check the AST. The CSV files have one column.
* `AntlrCompatibilityTests.cs` makes sure that the Rolex lexer gives the same tokens as the ANTLR lexer.

Use the [vscode-antlr4](https://github.com/mike-lischke/vscode-antlr4/blob/master/doc/grammar-debugging.md) extension to debug the ANTLR grammar.

### Generate the lexer

[Rolex](https://www.codeproject.com/Articles/5257489/Rolex-Unicode-Enabled-Lexer-Generator-in-Csharp) is a DFA lexer generator with the MIT license. Antlr2Rolex makes a Rolex grammar from the ANTLR grammar, and Rolex makes the DFA.

Prepare the Rolex grammars:

1. Run Antlr2Rolex on `src/ClosedXML.ANTLR/FormulaLexer.g4` with the A1 version. Write the result to `src/ClosedXML.Parser/Rolex/LexerA1.rl`.
2. Add `/*` at the start of the *Local A1 References* section. This removes `A1_CELL`, `A1_SPAN_REFERENCE` and their fragments.
3. Remove `/*` at the start of the *Local R1C1 References* section. This section has different tokens for `A1_CELL`, `A1_SPAN_REFERENCE` and their fragments.
4. Run Antlr2Rolex on `src/ClosedXML.ANTLR/FormulaLexer.g4` with the R1C1 version. Write the result to `src/ClosedXML.Parser/Rolex/LexerR1C1.rl`.

Fix the Rolex generator: Rolex does not read the `\u1234` escape (a Unicode escape with four hex digits) correctly. Add `pc.Advance()` to `_ParseEscapePart` and `_ParseRangeEscapePart` in `FFA.cs`.

Generate the DFA with Rolex:

```
Rolex.exe src\ClosedXML.Parser\Rolex\LexerA1.rl /noshared /output src\ClosedXML.Parser\Rolex\RolexA1Dfa.cs /namespace ClosedXML.Parser.Rolex
Rolex.exe src\ClosedXML.Parser\Rolex\LexerR1C1.rl /noshared /output src\ClosedXML.Parser\Rolex\RolexR1C1Dfa.cs /namespace ClosedXML.Parser.Rolex
```

## Resources

* [MS-XLSX](https://learn.microsoft.com/en-us/openspecs/office_standards/ms-xlsx/2c5dee00-eff2-4b22-92b6-0738acd4475e), chapter 2.2.2 Formulas
* [Simplified XLParser grammar](https://github.com/spreadsheetlab/XLParser/blob/master/doc/ebnf.pdf) and [tokens](https://github.com/spreadsheetlab/XLParser/blob/master/doc/tokens.pdf)
* [Getting Started With ANTLR in C#](https://tomassetti.me/getting-started-with-antlr-in-csharp/)
