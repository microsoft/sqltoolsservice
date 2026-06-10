//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Babel;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Language server protocol feature handlers that are driven purely by the
    /// T-SQL parser (no connection or binder metadata required): folding ranges,
    /// document symbols, semantic tokens, inlay hints and pull-model diagnostics.
    /// </summary>
    public partial class LanguageService
    {
        #region Folding Ranges

        /// <summary>
        /// Computes folding ranges for batches, statements and block comments.
        /// </summary>
        internal async Task HandleFoldingRangeRequest(
            FoldingRangeParams param,
            RequestContext<FoldingRange[]> requestContext)
        {
            FoldingRange[] result = Array.Empty<FoldingRange>();

            string uri = param?.TextDocument?.Uri;
            if (!string.IsNullOrEmpty(uri) && !ShouldSkipIntellisense(uri))
            {
                ScriptFile scriptFile = CurrentWorkspace.GetFile(uri);
                if (scriptFile != null)
                {
                    result = ComputeFoldingRanges(scriptFile.Contents);
                }
            }

            await requestContext.SendResult(result);
        }

        private FoldingRange[] ComputeFoldingRanges(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return Array.Empty<FoldingRange>();
            }

            ParseResult parseResult = Parser.Parse(sql, this.DefaultParseOptions);
            if (parseResult?.Script == null)
            {
                return Array.Empty<FoldingRange>();
            }

            var ranges = new List<FoldingRange>();
            var seen = new HashSet<(int, int)>();

            void AddRange(int startLine1Based, int endLine1Based, string kind)
            {
                int startLine = startLine1Based - 1;
                int endLine = endLine1Based - 1;
                if (endLine > startLine && seen.Add((startLine, endLine)))
                {
                    ranges.Add(new FoldingRange
                    {
                        StartLine = startLine,
                        EndLine = endLine,
                        Kind = kind
                    });
                }
            }

            // Batches and their statements. Statements are walked recursively so
            // that multi-line statements nested inside a block (e.g. the body of a
            // TRY/CATCH, an IF, a WHILE, or a stored-procedure definition) each get
            // their own folding range rather than only the outermost statement.
            if (parseResult.Script.Batches != null)
            {
                foreach (SqlBatch batch in parseResult.Script.Batches)
                {
                    // The parser makes consecutive batches share the GO separator line
                    // (batch N ends on the GO line and batch N+1 starts on it), so
                    // folding by batch.StartLocation/EndLocation would place the chevron
                    // on the GO line. Derive the batch range from its statements' span
                    // instead, which excludes the GO boundaries and skips batches that
                    // contain only a trailing GO.
                    if (batch.Statements != null)
                    {
                        int batchStart = int.MaxValue;
                        int batchEnd = int.MinValue;
                        foreach (SqlStatement statement in batch.Statements)
                        {
                            batchStart = Math.Min(batchStart, statement.StartLocation.LineNumber);
                            batchEnd = Math.Max(batchEnd, statement.EndLocation.LineNumber);
                            CollectStatementFolds(statement, AddRange);
                        }

                        if (batchEnd > batchStart)
                        {
                            AddRange(batchStart, batchEnd, null);
                        }
                    }
                }
            }

            // Multi-line block comments.
            var tokenManager = parseResult.Script.TokenManager;
            if (parseResult.Script.Tokens != null && tokenManager != null)
            {
                foreach (Token token in parseResult.Script.Tokens)
                {
                    if (tokenManager.IsTokenComment(token.Id))
                    {
                        AddRange(token.StartLocation.LineNumber, token.EndLocation.LineNumber, FoldingRangeKind.Comment);
                    }
                }
            }

            return ranges.ToArray();
        }

        /// <summary>
        /// Recursively emits a folding range for every multi-line statement in the
        /// subtree rooted at <paramref name="node"/>. Only statement nodes are
        /// folded (not expressions or clauses) to keep the fold set meaningful.
        /// </summary>
        private static void CollectStatementFolds(SqlCodeObject node, Action<int, int, string> addRange)
        {
            if (node == null)
            {
                return;
            }

            if (node is SqlStatement)
            {
                addRange(node.StartLocation.LineNumber, node.EndLocation.LineNumber, null);
            }

            foreach (SqlCodeObject child in node.Children)
            {
                CollectStatementFolds(child, addRange);
            }
        }

        #endregion

        #region Document Symbols

        /// <summary>
        /// Returns the top-level DDL symbols (CREATE/ALTER objects) in a document.
        /// </summary>
        internal async Task HandleDocumentSymbolRequest(
            DocumentSymbolParams param,
            RequestContext<SymbolInformation[]> requestContext)
        {
            SymbolInformation[] result = Array.Empty<SymbolInformation>();

            string uri = param?.TextDocument?.Uri;
            if (!string.IsNullOrEmpty(uri) && !ShouldSkipIntellisense(uri))
            {
                ScriptFile scriptFile = CurrentWorkspace.GetFile(uri);
                if (scriptFile != null)
                {
                    result = ComputeDocumentSymbols(scriptFile.Contents, uri);
                }
            }

            await requestContext.SendResult(result);
        }

        private SymbolInformation[] ComputeDocumentSymbols(string sql, string uri)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return Array.Empty<SymbolInformation>();
            }

            ParseResult parseResult = Parser.Parse(sql, this.DefaultParseOptions);
            if (parseResult?.Script?.Batches == null)
            {
                return Array.Empty<SymbolInformation>();
            }

            var symbols = new List<SymbolInformation>();
            foreach (SqlBatch batch in parseResult.Script.Batches)
            {
                if (batch.Statements == null)
                {
                    continue;
                }

                foreach (SqlStatement statement in batch.Statements)
                {
                    string typeName = statement.GetType().Name;
                    if (!typeName.StartsWith("SqlCreate", StringComparison.Ordinal)
                        && !typeName.StartsWith("SqlAlter", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string name = FindFirstIdentifierName(statement);
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    symbols.Add(new SymbolInformation
                    {
                        Name = name,
                        Kind = MapStatementToSymbolKind(typeName),
                        Location = new Location
                        {
                            Uri = uri,
                            Range = ToRange(statement.StartLocation, statement.EndLocation)
                        }
                    });
                }
            }

            return symbols.ToArray();
        }

        /// <summary>
        /// Recursively finds the first object/identifier name underneath a code object.
        /// </summary>
        private static string FindFirstIdentifierName(SqlCodeObject node)
        {
            if (node == null)
            {
                return null;
            }

            foreach (SqlCodeObject child in node.Children)
            {
                if (child is SqlObjectIdentifier || child is SqlIdentifier)
                {
                    return TextUtilities.RemoveSquareBracketSyntax(child.Sql);
                }

                string nested = FindFirstIdentifierName(child);
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Maps a CREATE/ALTER statement type name to the closest LSP symbol kind.
        /// </summary>
        private static SymbolKind MapStatementToSymbolKind(string typeName)
        {
            if (typeName.Contains("Procedure")) return SymbolKind.Method;
            if (typeName.Contains("Function")) return SymbolKind.Function;
            if (typeName.Contains("View")) return SymbolKind.Class;
            if (typeName.Contains("Table")) return SymbolKind.Class;
            if (typeName.Contains("Trigger")) return SymbolKind.Event;
            if (typeName.Contains("Schema")) return SymbolKind.Namespace;
            if (typeName.Contains("Index")) return SymbolKind.Property;
            if (typeName.Contains("Type")) return SymbolKind.Struct;
            if (typeName.Contains("Login") || typeName.Contains("User") || typeName.Contains("Role"))
            {
                return SymbolKind.Object;
            }

            return SymbolKind.Variable;
        }

        #endregion

        #region Semantic Tokens

        /// <summary>
        /// Computes semantic tokens for a full document using the SQL parser's
        /// line colorizer (the same classifier SSMS uses), mapped to colors that
        /// match SSMS: keywords, comments, strings, operators, system tables,
        /// functions and SQLCMD commands are colored, while identifiers, numbers
        /// and plain text are left in the default editor foreground.
        /// </summary>
        internal async Task HandleSemanticTokensRequest(
            SemanticTokensParams param,
            RequestContext<SemanticTokens> requestContext)
        {
            var result = new SemanticTokens { Data = Array.Empty<int>() };

            string uri = param?.TextDocument?.Uri;
            if (!string.IsNullOrEmpty(uri) && !ShouldSkipIntellisense(uri))
            {
                ScriptFile scriptFile = CurrentWorkspace.GetFile(uri);
                if (scriptFile != null)
                {
                    result.Data = ComputeSemanticTokens(scriptFile.Contents);
                }
            }

            await requestContext.SendResult(result);
        }

        private int[] ComputeSemanticTokens(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return Array.Empty<int>();
            }

            // The colorizer scans a line at a time and threads a colorization
            // "state" across lines so multi-line comments and strings are handled
            // correctly. A fresh scanner is created per request to keep the
            // request-local state isolated (handlers run in parallel).
            var scanner = new LineScanner
            {
                BatchSeparator = LanguageService.DefaultBatchSeperator
            };
            var tokenInfo = new TokenInfo();

            // LSP delta-encoding: 5 ints per token (deltaLine, deltaStartChar, length, type, modifiers).
            var data = new List<int>();
            int prevLine = 0;
            int prevChar = 0;
            int colorState = 0;

            string[] lines = sql.Split('\n');
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string lineText = lines[lineIndex].TrimEnd('\r');
                if (lineText.Length == 0)
                {
                    continue;
                }

                scanner.SetSource(lineText, 0);
                while (scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref colorState))
                {
                    int startChar = tokenInfo.StartIndex;
                    // Babel end index is inclusive; clamp to the line length.
                    int endChar = Math.Min(tokenInfo.EndIndex, lineText.Length - 1);
                    int length = endChar - startChar + 1;
                    if (length <= 0 || startChar < 0 || startChar >= lineText.Length)
                    {
                        continue;
                    }

                    int type = MapBabelTokenType(tokenInfo.Type);
                    if (type < 0)
                    {
                        continue;
                    }

                    int deltaLine = lineIndex - prevLine;
                    int deltaChar = deltaLine == 0 ? startChar - prevChar : startChar;
                    data.Add(deltaLine);
                    data.Add(deltaChar);
                    data.Add(length);
                    data.Add(type);
                    data.Add(0);
                    prevLine = lineIndex;
                    prevChar = startChar;
                }
            }

            return data.ToArray();
        }

        /// <summary>
        /// Maps a colorizer token classification to a semantic token type index,
        /// or returns -1 to leave the token uncolored (default editor foreground).
        /// The mapping mirrors the SSMS colorizer's Babel.TokenType color table
        /// (RadLangSvc Configuration.cs) so highlighting matches SSMS: identifiers,
        /// numbers and plain text render in the default foreground, while keywords,
        /// comments, strings, operators, system tables, functions and SQLCMD
        /// commands are colored.
        /// </summary>
        private static int MapBabelTokenType(TokenType type)
        {
            switch (type)
            {
                case TokenType.Keyword:
                    return SemanticTokensLegend.Keyword;
                case TokenType.Comment:
                    return SemanticTokensLegend.Comment;
                case TokenType.SqlString:
                    return SemanticTokensLegend.String;
                case TokenType.SqlOperator:
                case TokenType.Delimiter:
                    // SSMS colors symbolic operators, word operators (CROSS, JOIN,
                    // AND, ...) and delimiters (., ',' ( )) all with the single
                    // "SQL Operator" color. The scanner classifies word operators as
                    // SqlOperator as well, so they follow the same path.
                    return SemanticTokensLegend.Operator;
                case TokenType.SqlSystemTable:
                    return SemanticTokensLegend.Class;
                case TokenType.SqlSystemFunction:
                case TokenType.SqlStoredProcedure:
                    // SSMS uses distinct magenta/maroon colors here; "function" is
                    // the closest single semantic type the legend provides.
                    return SemanticTokensLegend.Function;
                case TokenType.SqlCmdCommand:
                    return SemanticTokensLegend.Macro;
                default:
                    // Identifier (@vars, [brackets], object names), Number, Text and
                    // Error are rendered by SSMS in the default editor foreground.
                    return -1;
            }
        }

        #endregion

        #region Inlay Hints

        /// <summary>
        /// Returns inline hints labeling the closing END of multi-line BEGIN/END blocks.
        /// </summary>
        internal async Task HandleInlayHintRequest(
            InlayHintParams param,
            RequestContext<InlayHint[]> requestContext)
        {
            InlayHint[] result = Array.Empty<InlayHint>();

            string uri = param?.TextDocument?.Uri;
            if (!string.IsNullOrEmpty(uri) && !ShouldSkipIntellisense(uri))
            {
                ScriptFile scriptFile = CurrentWorkspace.GetFile(uri);
                if (scriptFile != null)
                {
                    result = ComputeInlayHints(scriptFile.Contents, param.Range);
                }
            }

            await requestContext.SendResult(result);
        }

        private InlayHint[] ComputeInlayHints(string sql, Range range)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return Array.Empty<InlayHint>();
            }

            ParseResult parseResult = Parser.Parse(sql, this.DefaultParseOptions);
            if (parseResult?.Script == null)
            {
                return Array.Empty<InlayHint>();
            }

            bool hasRange = range.Start != null && range.End != null;
            int rangeStartLine = hasRange ? range.Start.Line : 0;
            int rangeEndLine = hasRange ? range.End.Line : int.MaxValue;

            var hints = new List<InlayHint>();
            WalkForBlockHints(parseResult.Script, null, hints, rangeStartLine, rangeEndLine);
            return hints.ToArray();
        }

        /// <summary>
        /// Recursively walks the AST adding a hint at the END of each multi-line
        /// compound (BEGIN/END) block, labeled by its enclosing construct.
        /// </summary>
        private static void WalkForBlockHints(
            SqlCodeObject node,
            SqlCodeObject parent,
            List<InlayHint> hints,
            int rangeStartLine,
            int rangeEndLine)
        {
            if (node == null)
            {
                return;
            }

            if (node is SqlCompoundStatement
                && node.EndLocation.LineNumber > node.StartLocation.LineNumber)
            {
                int endLine = node.EndLocation.LineNumber - 1;
                if (endLine >= rangeStartLine && endLine <= rangeEndLine)
                {
                    string construct = DescribeBlockConstruct(parent);
                    hints.Add(new InlayHint
                    {
                        Position = new Position
                        {
                            Line = endLine,
                            Character = node.EndLocation.ColumnNumber - 1
                        },
                        Label = construct,
                        PaddingLeft = true,
                        Tooltip = $"End of {construct} block"
                    });
                }
            }

            foreach (SqlCodeObject child in node.Children)
            {
                WalkForBlockHints(child, node, hints, rangeStartLine, rangeEndLine);
            }
        }

        /// <summary>
        /// Derives a human-readable construct name for a BEGIN/END block from its parent.
        /// </summary>
        private static string DescribeBlockConstruct(SqlCodeObject parent)
        {
            string parentName = parent?.GetType().Name ?? string.Empty;
            if (parentName.Contains("While")) return "while";
            if (parentName.Contains("If")) return "if";
            if (parentName.Contains("Try")) return "try";
            if (parentName.Contains("Catch")) return "catch";
            return "begin";
        }

        #endregion

        #region Pull Diagnostics

        /// <summary>
        /// Returns a full pull-model diagnostic report (syntax + semantic markers).
        /// </summary>
        internal async Task HandleDocumentDiagnosticRequest(
            DocumentDiagnosticParams param,
            RequestContext<FullDocumentDiagnosticReport> requestContext)
        {
            var report = new FullDocumentDiagnosticReport { Items = Array.Empty<Diagnostic>() };

            string uri = param?.TextDocument?.Uri;
            if (!string.IsNullOrEmpty(uri) && !ShouldSkipIntellisense(uri))
            {
                ScriptFile scriptFile = CurrentWorkspace.GetFile(uri);
                if (scriptFile != null)
                {
                    ScriptFileMarker[] semanticMarkers = await GetSemanticMarkers(scriptFile);
                    IEnumerable<ScriptFileMarker> allMarkers = scriptFile.SyntaxMarkers != null
                        ? scriptFile.SyntaxMarkers.Concat(semanticMarkers)
                        : semanticMarkers;

                    report.Items = allMarkers
                        .Select(DiagnosticsHelper.GetDiagnosticFromMarker)
                        .ToArray();
                }
            }

            await requestContext.SendResult(report);
        }

        #endregion

        /// <summary>
        /// Builds a zero-based LSP range from a pair of 1-based parser locations.
        /// </summary>
        private static Range ToRange(
            Microsoft.SqlServer.Management.SqlParser.Parser.Location start,
            Microsoft.SqlServer.Management.SqlParser.Parser.Location end)
        {
            return new Range
            {
                Start = new Position
                {
                    Line = start.LineNumber - 1,
                    Character = start.ColumnNumber - 1
                },
                End = new Position
                {
                    Line = end.LineNumber - 1,
                    Character = end.ColumnNumber - 1
                }
            };
        }
    }
}
