//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlTools.Utility;
using Location = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Location;
using Position = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Position;
using Range = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// ScriptDom-based syntactic analysis for the rename / find-all-references feature.
    ///
    /// While DacFx answers the semantic question "which files reference this object?", this helper
    /// answers the syntactic question "where, exactly, does the name appear in each file?" by
    /// walking a real T-SQL abstract syntax tree.  Using the AST (instead of a flat token scan)
    /// lets us target only the spans that actually denote the object — identifiers and the
    /// level-name arguments of the extended-property stored procedures — so unrelated string
    /// literals are never rewritten.
    /// </summary>
    internal static class RenameScriptDomHelper
    {
        /// <summary>The extended-property stored procedures whose level-name arguments name an object.</summary>
        private static readonly HashSet<string> ExtendedPropertyProcs = new(StringComparer.OrdinalIgnoreCase)
        {
            "sp_addextendedproperty",
            "sp_updateextendedproperty",
            "sp_dropextendedproperty"
        };

        /// <summary>
        /// Resolves the name the cursor is on by walking the ScriptDom token stream of
        /// <paramref name="sqlText"/>.  Produces the bare clicked name and, when the name is part of
        /// a dotted reference, the full schema-qualified name (e.g. <c>dbo.Customers.Id</c>).
        /// </summary>
        /// <param name="sqlText">The full text of the editor buffer.</param>
        /// <param name="line0">0-based cursor line.</param>
        /// <param name="col0">0-based cursor column.</param>
        /// <param name="bareName">The unquoted name under the cursor.</param>
        /// <param name="qualifiedName">The dotted name including any preceding segments, or <c>null</c>.</param>
        public static bool TryResolveCursorName(string sqlText, int line0, int col0, out string bareName, out string qualifiedName)
        {
            bareName = null;
            qualifiedName = null;
            if (string.IsNullOrEmpty(sqlText))
                return false;

            IList<TSqlParserToken> tokens = ParseTokens(sqlText);
            if (tokens == null)
                return false;

            // ScriptDom lines/columns are 1-based; LSP positions are 0-based.
            int line = line0 + 1;
            int col = col0 + 1;

            int index = FindTokenIndexAt(tokens, line, col);
            if (index < 0)
                return false;

            bareName = UnquoteName(tokens[index]);
            if (string.IsNullOrWhiteSpace(bareName))
                return false;

            // Reconstruct any preceding dotted segments (schema/table prefixes).
            var prefixParts = new List<string>();
            int j = index - 1;
            while (j >= 0)
            {
                while (j >= 0 && IsTrivia(tokens[j])) j--;
                if (j < 0 || tokens[j].TokenType != TSqlTokenType.Dot) break;

                j--; // consume the dot
                while (j >= 0 && IsTrivia(tokens[j])) j--;
                if (j < 0 || !IsNameToken(tokens[j])) break;

                prefixParts.Insert(0, UnquoteName(tokens[j]));
                j--;
            }

            qualifiedName = prefixParts.Count > 0
                ? string.Join(".", prefixParts) + "." + bareName
                : null;
            return true;
        }

        /// <summary>
        /// Returns a <see cref="Location"/> for every spot in <paramref name="filePath"/> where the
        /// object named <paramref name="objectName"/> appears, by walking the file's ScriptDom AST.
        /// Matches both identifiers and the level-name arguments of the extended-property procedures.
        /// Reads the file content from disk — the same source the DacFx model is built from.
        /// </summary>
        public static IEnumerable<Location> FindNameLocationsInFile(string filePath, string objectName)
        {
            // Read directly from disk to stay consistent with the DacFx model, which is built from
            // the saved project files. The workspace buffer cache is not used here because it can
            // hold duplicate entries for the same path under different URI encodings.
            string sqlText = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            if (sqlText == null)
                return Array.Empty<Location>();

            TSqlFragment fragment = ParseFragment(sqlText);
            if (fragment == null)
                return Array.Empty<Location>();

            string fileUri = new Uri(filePath).AbsoluteUri;
            var visitor = new RenameLocationVisitor(objectName, fileUri);
            fragment.Accept(visitor);
            return visitor.Locations;
        }

        /// <summary>
        /// Returns a <see cref="MoveSchemaEdit"/> for every schema-qualified (or unqualified)
        /// reference to <paramref name="objectName"/> in <paramref name="filePath"/> that resolves
        /// to the <paramref name="oldSchema"/> schema. Each edit either rewrites an existing schema
        /// qualifier to <paramref name="newSchema"/> or inserts the new qualifier before an
        /// unqualified name. Reads the file content from disk — the same source the DacFx model is
        /// built from.
        /// </summary>
        public static IEnumerable<MoveSchemaEdit> FindSchemaMoveEditsInFile(
            string filePath, string objectName, string oldSchema, string newSchema)
        {
            string sqlText = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            if (sqlText == null)
                return Array.Empty<MoveSchemaEdit>();

            TSqlFragment fragment = ParseFragment(sqlText);
            if (fragment == null)
                return Array.Empty<MoveSchemaEdit>();

            string fileUri = new Uri(filePath).AbsoluteUri;
            var visitor = new SchemaMoveLocationVisitor(objectName, oldSchema, newSchema, fileUri);
            fragment.Accept(visitor);
            return visitor.Edits;
        }

        private static TSql160Parser CreateParser() => new TSql160Parser(initialQuotedIdentifiers: true);

        /// <summary>Parses <paramref name="sqlText"/> and returns its token stream, or <c>null</c>.</summary>
        private static IList<TSqlParserToken> ParseTokens(string sqlText)
        {
            using var reader = new StringReader(sqlText);
            TSqlFragment fragment = CreateParser().Parse(reader, out _);
            return fragment?.ScriptTokenStream;
        }

        /// <summary>Parses <paramref name="sqlText"/> into an AST fragment, or <c>null</c>.</summary>
        private static TSqlFragment ParseFragment(string sqlText)
        {
            using var reader = new StringReader(sqlText);
            return CreateParser().Parse(reader, out _);
        }

        /// <summary>Finds the index of the token covering the 1-based <paramref name="line"/>/<paramref name="col"/>.</summary>
        private static int FindTokenIndexAt(IList<TSqlParserToken> tokens, int line, int col)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Line != line || string.IsNullOrEmpty(t.Text) || !IsNameOrLiteral(t))
                    continue;

                int length = t.Text.Length;
                // Inclusive of the trailing edge so a cursor at the end of a name still matches.
                if (col >= t.Column && col <= t.Column + length)
                    return i;
            }
            return -1;
        }

        private static bool IsTrivia(TSqlParserToken token) =>
            token.TokenType == TSqlTokenType.WhiteSpace ||
            token.TokenType == TSqlTokenType.SingleLineComment ||
            token.TokenType == TSqlTokenType.MultilineComment;

        private static bool IsNameToken(TSqlParserToken token) =>
            token.TokenType == TSqlTokenType.Identifier ||
            token.TokenType == TSqlTokenType.QuotedIdentifier;

        private static bool IsNameOrLiteral(TSqlParserToken token) =>
            IsNameToken(token) ||
            token.TokenType == TSqlTokenType.AsciiStringLiteral ||
            token.TokenType == TSqlTokenType.UnicodeStringLiteral;

        /// <summary>
        /// Returns the (role, parameter) pairs of an extended-property stored-procedure call, or
        /// <c>null</c> when <paramref name="node"/> is not such a call. The role is the parameter's
        /// explicit <c>@name</c> (without the <c>@</c>) or, for positional calls, the name from the
        /// procedure's fixed signature. Shared by the rename and move visitors so the argument
        /// layout is parsed in one place.
        /// </summary>
        private static List<(string Role, ExecuteParameter Param)> GetExtendedPropertyArgs(ExecuteStatement node)
        {
            if (node.ExecuteSpecification?.ExecutableEntity is not ExecutableProcedureReference proc)
                return null;

            string procName = proc.ProcedureReference?.ProcedureReference?.Name?.BaseIdentifier?.Value;
            if (procName == null || !ExtendedPropertyProcs.Contains(procName))
                return null;

            bool isDrop = string.Equals(procName, "sp_dropextendedproperty", StringComparison.OrdinalIgnoreCase);
            // Positional argument layout differs because the drop procedure has no @value.
            string[] signature = isDrop
                ? new[] { "name", "level0type", "level0name", "level1type", "level1name", "level2type", "level2name" }
                : new[] { "name", "value", "level0type", "level0name", "level1type", "level1name", "level2type", "level2name" };

            var args = new List<(string, ExecuteParameter)>();
            for (int i = 0; i < proc.Parameters.Count; i++)
            {
                ExecuteParameter param = proc.Parameters[i];
                string role = param.Variable?.Name?.TrimStart('@')
                    ?? (i < signature.Length ? signature[i] : null);
                if (role != null)
                    args.Add((role, param));
            }
            return args;
        }

        /// <summary>Strips surrounding brackets, double-quotes, or string-literal quotes from a token.</summary>
        private static string UnquoteName(TSqlParserToken token)
        {
            string text = token.Text ?? string.Empty;
            if (token.TokenType == TSqlTokenType.AsciiStringLiteral ||
                token.TokenType == TSqlTokenType.UnicodeStringLiteral)
            {
                int start = (text.Length > 0 && (text[0] == 'N' || text[0] == 'n')) ? 1 : 0;
                if (text.Length >= start + 2 && text[start] == '\'' && text[text.Length - 1] == '\'')
                {
                    return text.Substring(start + 1, text.Length - start - 2).Replace("''", "'");
                }
                return text;
            }

            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
                return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");

            return TextUtilities.RemoveSquareBracketSyntax(text);
        }

        /// <summary>
        /// AST visitor that collects the source spans naming a specific object: every matching
        /// identifier, plus the level-name string-literal arguments of the extended-property
        /// stored procedures.
        /// </summary>
        private sealed class RenameLocationVisitor : TSqlFragmentVisitor
        {
            private readonly string objectName;
            private readonly string fileUri;

            public List<Location> Locations { get; } = new();

            public RenameLocationVisitor(string objectName, string fileUri)
            {
                this.objectName = objectName;
                this.fileUri = fileUri;
            }

            public override void Visit(Identifier node)
            {
                if (string.Equals(node.Value, this.objectName, StringComparison.OrdinalIgnoreCase))
                {
                    // Emit the span of the source token itself (which includes any surrounding
                    // brackets or quotes) so bracket-quoting can be re-applied to the replacement
                    // text. The AST node's fragment span excludes the brackets for quoted
                    // identifiers, which would defeat that detection.
                    TSqlParserToken token = node.ScriptTokenStream[node.FirstTokenIndex];
                    AddSpan(token.Line, token.Column, token.Text?.Length ?? 0);
                }
            }

            public override void Visit(ExecuteStatement node)
            {
                var args = GetExtendedPropertyArgs(node);
                if (args == null)
                    return;

                foreach (var (role, param) in args)
                {
                    if (!IsLevelNameRole(role))
                        continue;

                    if (param.ParameterValue is StringLiteral literal &&
                        string.Equals(literal.Value, this.objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Emit only the inner text span, skipping the optional N prefix and the quotes,
                        // so a rename rewrites the name and leaves the literal syntax intact.
                        int offset = literal.IsNational ? 2 : 1;
                        int innerLength = literal.FragmentLength - (literal.IsNational ? 3 : 2);
                        AddSpan(literal.StartLine, literal.StartColumn + offset, innerLength);
                    }
                }
            }

            private static bool IsLevelNameRole(string role) =>
                string.Equals(role, "level0name", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "level1name", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "level2name", StringComparison.OrdinalIgnoreCase);

            /// <summary>Adds a location for a span defined by a 1-based start position and length.</summary>
            private void AddSpan(int startLine1, int startColumn1, int length)
            {
                if (length <= 0)
                    return;

                int line = startLine1 - 1;
                int startChar = startColumn1 - 1;
                this.Locations.Add(new Location
                {
                    Uri = this.fileUri,
                    Range = new Range
                    {
                        Start = new Position { Line = line, Character = startChar },
                        End = new Position { Line = line, Character = startChar + length }
                    }
                });
            }
        }

        /// <summary>A single text edit produced by the move-to-schema scan: the source span and its replacement.</summary>
        public sealed class MoveSchemaEdit
        {
            public Location Location { get; set; }
            public string NewText { get; set; }
        }

        /// <summary>
        /// AST visitor that collects the schema-qualifier edits needed to move an object to a new
        /// schema. For each <see cref="SchemaObjectName"/> whose base identifier matches the target
        /// object and that resolves to the old schema, it either rewrites the existing schema
        /// qualifier or inserts the new qualifier before an unqualified name.
        /// </summary>
        private sealed class SchemaMoveLocationVisitor : TSqlFragmentVisitor
        {
            private readonly string objectName;
            private readonly string oldSchema;
            private readonly string newSchemaBracketed;
            private readonly string newSchemaPlain;
            private readonly string fileUri;

            public List<MoveSchemaEdit> Edits { get; } = new();

            public SchemaMoveLocationVisitor(string objectName, string oldSchema, string newSchema, string fileUri)
            {
                this.objectName = objectName;
                this.oldSchema = oldSchema;
                // SQL bracket quoting requires a literal "]" inside an identifier to be escaped as "]]".
                this.newSchemaBracketed = $"[{newSchema.Replace("]", "]]")}]";
                // Bare schema name for the extended-property string literal (e.g. N'Sales'), which is
                // not a bracket-quoted identifier.
                this.newSchemaPlain = newSchema;
                this.fileUri = fileUri;
            }

            public override void Visit(SchemaObjectName node)
            {
                if (!string.Equals(node.BaseIdentifier?.Value, this.objectName, StringComparison.OrdinalIgnoreCase))
                    return;

                if (node.SchemaIdentifier != null)
                {
                    // Two-part (or more) name — only rewrite when it points at the old schema.
                    if (!string.Equals(node.SchemaIdentifier.Value, this.oldSchema, StringComparison.OrdinalIgnoreCase))
                        return;

                    TSqlParserToken schemaToken = node.SchemaIdentifier.ScriptTokenStream[node.SchemaIdentifier.FirstTokenIndex];
                    AddReplace(schemaToken.Line, schemaToken.Column, schemaToken.Text?.Length ?? 0, this.newSchemaBracketed);
                }
                else
                {
                    // Unqualified name — resolves to the old schema by default. Qualify it with the
                    // new schema so the reference keeps pointing at the moved object.
                    TSqlParserToken baseToken = node.BaseIdentifier.ScriptTokenStream[node.BaseIdentifier.FirstTokenIndex];
                    AddInsert(baseToken.Line, baseToken.Column, this.newSchemaBracketed + ".");
                }
            }

            /// <summary>
            /// Rewrites the schema named by <c>@level0name</c> in an extended-property call that
            /// describes the moved object (its <c>@level1name</c> is the object and <c>@level0type</c>
            /// is SCHEMA). Without this, the extended property keeps pointing at the old schema after
            /// the object moves.
            /// </summary>
            public override void Visit(ExecuteStatement node)
            {
                var args = GetExtendedPropertyArgs(node);
                if (args == null)
                    return;

                string level0Type = null;
                StringLiteral level0Name = null;
                StringLiteral level1Name = null;
                foreach (var (role, param) in args)
                {
                    if (param.ParameterValue is not StringLiteral lit)
                        continue;
                    if (string.Equals(role, "level0type", StringComparison.OrdinalIgnoreCase))
                        level0Type = lit.Value;
                    else if (string.Equals(role, "level0name", StringComparison.OrdinalIgnoreCase))
                        level0Name = lit;
                    else if (string.Equals(role, "level1name", StringComparison.OrdinalIgnoreCase))
                        level1Name = lit;
                }

                if (level0Name != null && level1Name != null &&
                    string.Equals(level0Type, "SCHEMA", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(level1Name.Value, this.objectName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(level0Name.Value, this.oldSchema, StringComparison.OrdinalIgnoreCase))
                {
                    // Replace only the inner text of the literal, leaving the optional N prefix and
                    // the surrounding quotes intact.
                    int offset = level0Name.IsNational ? 2 : 1;
                    int innerLength = level0Name.FragmentLength - (level0Name.IsNational ? 3 : 2);
                    AddReplace(level0Name.StartLine, level0Name.StartColumn + offset, innerLength, this.newSchemaPlain);
                }
            }

            /// <summary>Adds a replacement edit over a span defined by a 1-based start position and length.</summary>
            private void AddReplace(int startLine1, int startColumn1, int length, string newText)
            {
                if (length <= 0)
                    return;

                int line = startLine1 - 1;
                int startChar = startColumn1 - 1;
                this.Edits.Add(new MoveSchemaEdit
                {
                    NewText = newText,
                    Location = new Location
                    {
                        Uri = this.fileUri,
                        Range = new Range
                        {
                            Start = new Position { Line = line, Character = startChar },
                            End = new Position { Line = line, Character = startChar + length }
                        }
                    }
                });
            }

            /// <summary>Adds a zero-width insertion edit at a 1-based start position.</summary>
            private void AddInsert(int startLine1, int startColumn1, string newText)
            {
                int line = startLine1 - 1;
                int startChar = startColumn1 - 1;
                this.Edits.Add(new MoveSchemaEdit
                {
                    NewText = newText,
                    Location = new Location
                    {
                        Uri = this.fileUri,
                        Range = new Range
                        {
                            Start = new Position { Line = line, Character = startChar },
                            End = new Position { Line = line, Character = startChar }
                        }
                    }
                });
            }
        }
    }
}
