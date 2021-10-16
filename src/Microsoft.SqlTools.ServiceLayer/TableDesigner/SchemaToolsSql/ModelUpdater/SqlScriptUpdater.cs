//------------------------------------------------------------------------------
// <copyright file="SqlScriptUpdater.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.ModelUpdater;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using CodeGenerationSupporter = Microsoft.Data.Tools.Schema.ScriptDom.Sql.CodeGenerationSupporter;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal class SqlScriptUpdater
    {
        private delegate Identifier GetElementIdentifierAst(TSqlFragment elementAst);
        private static Dictionary<ModelElementClass, GetElementIdentifierAst> _getElementIdentifierAction;

        private readonly SqlScriptGenerator _scriptGenerator;

        static SqlScriptUpdater()
        {
            InitializeGetElementIdentifierActions();
        }

        public SqlScriptUpdater(SqlDatabaseSchemaProvider dsp)
        {
            SqlExceptionUtils.ValidateNullParameter(dsp, "dsp");

            SqlScriptGeneratorConstructor sgCtor = dsp.GetServiceConstructor<SqlScriptGeneratorConstructor>();
            _scriptGenerator = sgCtor.ConstructService();
        }

        private static void InitializeGetElementIdentifierActions()
        {
            // initialize a list of delegates for retrieving the base identifier
            // of an element from the element's primary AST

            _getElementIdentifierAction = new Dictionary<ModelElementClass, GetElementIdentifierAst>();

            // Table

            _getElementIdentifierAction[SqlTable.SqlTableClass] =
                (ast) => ((CreateTableStatement)ast).SchemaObjectName.BaseIdentifier;

            // Column

            GetElementIdentifierAst getColumnIdentifierAst = (ast) => ((ColumnDefinition)ast).ColumnIdentifier;

            _getElementIdentifierAction[SqlSimpleColumn.SqlSimpleColumnClass] =
                getColumnIdentifierAst;
            _getElementIdentifierAction[SqlComputedColumn.SqlComputedColumnClass] =
                getColumnIdentifierAst;
            _getElementIdentifierAction[SqlColumnSet.SqlColumnSetClass] =
                getColumnIdentifierAst;

            // Constraints

            GetElementIdentifierAst getConstraintIdentifierAst =
                (ast) =>
                {
                    AlterTableAddTableElementStatement alterTableAst = ast as AlterTableAddTableElementStatement;
                    if (alterTableAst != null)
                    {
                        Debug.Assert(
                            alterTableAst.Definition != null &&
                            alterTableAst.Definition.TableConstraints != null &&
                            alterTableAst.Definition.TableConstraints.Count == 1,
                            "We should have only one constraint inside an ALTER TABLE statement");

                        if (alterTableAst.Definition != null &&
                            alterTableAst.Definition.TableConstraints != null &&
                            alterTableAst.Definition.TableConstraints.Count == 1)
                        {
                            return alterTableAst.Definition.TableConstraints[0].ConstraintIdentifier;
                        }
                    }
                    else
                    {
                        return ((ConstraintDefinition)ast).ConstraintIdentifier;
                    }

                    return null;
                };

            _getElementIdentifierAction[SqlPrimaryKeyConstraint.SqlPrimaryKeyConstraintClass] =
                getConstraintIdentifierAst;
            _getElementIdentifierAction[SqlUniqueConstraint.SqlUniqueConstraintClass] =
                getConstraintIdentifierAst;
            _getElementIdentifierAction[SqlForeignKeyConstraint.SqlForeignKeyConstraintClass] =
                getConstraintIdentifierAst;
            _getElementIdentifierAction[SqlCheckConstraint.SqlCheckConstraintClass] =
                getConstraintIdentifierAst;

            // Indexes

            GetElementIdentifierAst getIndexIdentifierAst = (ast) => ((IndexStatement)ast).Name;

            _getElementIdentifierAction[SqlIndex.SqlIndexClass] = getIndexIdentifierAst;
            _getElementIdentifierAction[SqlXmlIndex.SqlXmlIndexClass] = getIndexIdentifierAst;
            _getElementIdentifierAction[SqlSelectiveXmlIndex.SqlSelectiveXmlIndexClass] = getIndexIdentifierAst;

            _getElementIdentifierAction[SqlSpatialIndex.SqlSpatialIndexClass] =
                (ast) => ((CreateSpatialIndexStatement)ast).Name;

            // DML Trigger

            _getElementIdentifierAction[SqlDmlTrigger.SqlDmlTriggerClass] =
                (ast) => ((CreateTriggerStatement)ast).Name.BaseIdentifier;
        }

        private static TAstType GetElementAst<TAstType>(ISqlModelElement element) where TAstType : TSqlFragment
        {
            return element.PrimarySource == null ? null : SqlModelUpdaterUtils.GetPrimaryAst<TAstType>(element);
        }

        public static IList<SqlScriptUpdateInfo> SetColumnNullable(SqlSimpleColumn column, bool isNullable)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            if (column.GraphType == SqlColumnGraphType.None && column.IsNullable != isNullable)
            {
                ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
                NullableConstraintDefinition nullableAst = null;
                foreach (var constraint in columnAst.Constraints)
                {
                    nullableAst = constraint as NullableConstraintDefinition;
                    if (nullableAst != null)
                    {
                        break;
                    }
                }

                int startOffset = 0;
                int startLine = 0;
                int startColumn = 0;
                int fragmentLength = 0;
                string newText = null;
                if (nullableAst == null) // we don't have nullability in the original script
                {
                    TSqlParserToken nextToken = columnAst.ScriptTokenStream[columnAst.LastTokenIndex + 1];

                    PopulatePositionInfoFromToken(nextToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                    fragmentLength = 0;
                    newText =
                        isNullable ?
                        SqlModelUpdaterConstants.Space + ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Null) : // " NULL"
                        GenerateSpaceNotSpaceNull(); // " NOT NULL"
                }
                else // we do have nullability in the original script
                {
                    if (isNullable) // NOT NULL -> NULL
                    {
                        TSqlParserToken token = nullableAst.ScriptTokenStream[nullableAst.FirstTokenIndex];
                        if (token.TokenType != TSqlTokenType.Not)
                        {
                            // NOT was not found, this should not happend, let's throw
                            // this message isn't meant to be visible to customers, so use plain string (not resource string)
                            SqlModelUpdaterUtils.TraceAndThrow("NOT was not found for NOT NULL constraint");
                        }
                        else
                        {
                            // remove NOT and the following white spce
                            TSqlParserToken nextToken = nullableAst.ScriptTokenStream[nullableAst.FirstTokenIndex + 1];
                            if (nextToken.TokenType == TSqlTokenType.WhiteSpace)
                            {
                                nextToken = nullableAst.ScriptTokenStream[nullableAst.FirstTokenIndex + 2];
                            }

                            PopulatePositionInfoFromToken(token, out startOffset, out startLine, out startColumn, out fragmentLength);
                            fragmentLength = nextToken.Offset - startOffset;
                            newText = string.Empty;
                        }
                    }
                    else // NULL -> NOT NULL
                    {
                        // insert NOT before existing NULL
                        PopulatePositionInfoFromAst(nullableAst, out startOffset, out startLine, out startColumn, out fragmentLength);
                        fragmentLength = 0;
                        newText = ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Not) + SqlModelUpdaterConstants.Space; // "NOT "
                    }
                }

                AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);
            }

            DBG_ValidateUpdateItems(scriptUpdateList);

            return scriptUpdateList;
        }

        private static void AddScriptUpdateForElement(IList<SqlScriptUpdateInfo> scriptUpdateList, ISqlModelElement element, string newText, int startOffset, int startLine, int startColumn, int fragmentLength)
        {
            string cacheId = element.PrimarySource.SourceName;

            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(cacheId);
            scriptUpdate.AddUpdate(startOffset, startLine, startColumn, fragmentLength, newText);
            scriptUpdateList.Add(scriptUpdate);
        }

        private static void AddScriptUpdateForElementBeforeToken(IList<SqlScriptUpdateInfo> scriptUpdateList, ISqlModelElement element, string newText, TSqlParserToken token)
        {
            AddScriptUpdateForElement(scriptUpdateList, element, newText, token.Offset, token.Line, token.Column, 0);
        }

        private static void AddScriptUpdateForElementBetweenTokens(IList<SqlScriptUpdateInfo> scriptUpdateList, ISqlModelElement element, string newText, TSqlParserToken firstToken,
            TSqlParserToken lastToken)
        {
            AddScriptUpdateForElement(scriptUpdateList, element, newText, firstToken.Offset, firstToken.Line, firstToken.Column, lastToken.Offset - firstToken.Offset);
        }

        private static string GenerateSpaceNotSpaceNull()
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Not);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Null);
            return sfGen.GetScriptFragment();
        }

        public static IList<SqlScriptUpdateInfo> SetColumnIsIdentity(SqlSimpleColumn column, bool isIdentity)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            if (column.IsIdentity != isIdentity)
            {
                ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
                IdentityOptions identityOptions = columnAst.IdentityOptions;

                int startOffset;
                int startLine;
                int startColumn;
                int fragmentLength;
                string newText;
                if (identityOptions == null) // we don't have identity options in the original script
                {
                    if (!isIdentity)
                    {
                        SqlModelUpdaterUtils.TraceAndThrow("if this line of code is reached, we don’t have IdentityOptions AST, but the model says column.IsIdentity equals true.");
                    }

                    // "" -> " IDENTITY"

                    TSqlParserToken nextToken = columnAst.ScriptTokenStream[columnAst.LastTokenIndex + 1];
                    PopulatePositionInfoFromToken(nextToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                    fragmentLength = 0;
                    newText = SqlModelUpdaterConstants.Space + ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Identity); // " IDENTITY"
                }
                else // we do have identity options in the original script
                {
                    // remove identity specification, including seed/increment if specified
                    if (isIdentity)
                    {
                        SqlModelUpdaterUtils.TraceAndThrow("if this line of code is reached, we have IdentityOptions AST, but the model says column.IsIdentity equals false.");
                    }

                    // "IDENTITY [(seed,increment)]" -> ""

                    IList<TSqlParserToken> tokenStream = identityOptions.ScriptTokenStream;

                    TSqlParserToken firstToken = tokenStream[identityOptions.FirstTokenIndex];
                    TSqlParserToken nextToken = tokenStream[identityOptions.LastTokenIndex + 1];

                    // if the preceding token is space, remove it too
                    if (SqlModelUpdaterUtils.IsSpaceToken(tokenStream[identityOptions.FirstTokenIndex - 1]))
                    {
                        firstToken = tokenStream[identityOptions.FirstTokenIndex - 1];
                    }

                    PopulatePositionInfoFromToken(firstToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                    fragmentLength = nextToken.Offset - startOffset;
                    newText = string.Empty;
                }

                AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);
            }

            DBG_ValidateUpdateItems(scriptUpdateList);

            return scriptUpdateList;
        }

        public static IList<SqlScriptUpdateInfo> SetColumnIdentitySeed(SqlSimpleColumn column, SqlDecimal seed)
        {
            return SetColumnIdentitySeedOrIncrement(column, seed, true);
        }

        public static IList<SqlScriptUpdateInfo> SetColumnIdentityIncrement(SqlSimpleColumn column, SqlDecimal increment)
        {
            return SetColumnIdentitySeedOrIncrement(column, increment, false);
        }

        /// <summary>
        /// generates script changes required to change seed or increment for an identity column
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <param name="isSeedOrIncrement">true if value is seed, false if value is increment</param>
        /// <returns></returns>
        private static IList<SqlScriptUpdateInfo> SetColumnIdentitySeedOrIncrement(SqlSimpleColumn column, SqlDecimal value, bool isValueSeed)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            if (!column.IsIdentity)
            {
                SqlModelUpdaterUtils.TraceAndThrow("should not try to modify identity seed/increment if column is not identity");
            }

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            IdentityOptions identityOptions = columnAst.IdentityOptions;

            int startOffset;
            int startLine;
            int startColumn;
            int fragmentLength;
            string newText = value.ToString();
            if (identityOptions == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("identity options not found for identity column");
            }

            ScalarExpression valueAst = isValueSeed ? identityOptions.IdentitySeed : identityOptions.IdentityIncrement;

            if (valueAst == null) // "(<seed>, <increment>)" not yet specified in script, generate expression
            {
                int foundTokenIndex;
                TSqlParserToken foundToken;

                SqlModelUpdaterUtils.FindToken(identityOptions.ScriptTokenStream, identityOptions.FirstTokenIndex,
                    token => token.TokenType == TSqlTokenType.Identity, out foundTokenIndex, out foundToken);

                Debug.Assert(foundToken != null);

                startOffset = foundToken.Offset + foundToken.Text.Length;
                startLine = foundToken.Line;
                startColumn = foundToken.Column + foundToken.Text.Length;
                fragmentLength = 0;

                // if setting seed:      -> "(<new_seed>, <default_increment>)"
                // if setting increment: -> "(<default_seed>, <new_increment>)"

                newText = string.Format(CultureInfo.InvariantCulture, "({0}, {1})",
                    isValueSeed ? newText : column.IdentitySeed.ToString(),
                    isValueSeed ? column.IdentityIncrement.ToString() : newText);
            }
            else // (seed, increment) already specified in script, replace existing value
            {
                PopulatePositionInfoFromAst(valueAst, out startOffset, out startLine, out startColumn, out fragmentLength);
            }

            AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);

            DBG_ValidateUpdateItems(scriptUpdateList);

            return scriptUpdateList;
        }

        #region Public Methods that operate on Columns

        /// <summary>
        /// Generates the script changes required to change a column's data type.
        /// </summary>
        /// <returns>The list of changes required to modify the data type</returns>
        public IList<SqlScriptUpdateInfo> SetColumnDataType(SqlSimpleColumn column, SqlType dataType, out bool isNamelessTimestamp)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            DataTypeReference dataTypeAst = columnAst.DataType;

            int startOffset;
            int startLine;
            int startColumn;
            int fragmentLength;
            string newText;

            if (dataTypeAst == null)
            {
                // No datatype specifier. This is a nameless timestamp specification.

                // Get first token after column identifier
                TSqlParserToken token = columnAst.ScriptTokenStream[columnAst.ColumnIdentifier.LastTokenIndex + 1];
                PopulatePositionInfoFromToken(token, out startOffset, out startLine, out startColumn, out fragmentLength);

                // Insert new string, instead of replacing the token
                fragmentLength = 0;
                newText = SqlModelUpdaterConstants.Space + GenerateScriptForDataType(dataType);
                isNamelessTimestamp = true;
            }
            else
            {
                // we remove all the text for the data type
                // it seems to make little sense to preserve the embedded comments

                PopulatePositionInfoFromAst(dataTypeAst, out startOffset, out startLine, out startColumn, out fragmentLength);
                newText = GenerateScriptForDataType(dataType);
                isNamelessTimestamp = false;
            }

            AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);

            return scriptUpdateList;
        }

        /// <summary>
        /// Sets the length of the column's data type.
        /// </summary>
        /// <returns>The list of changes required to modify the data type</returns>
        public static IList<SqlScriptUpdateInfo> SetColumnDataTypeLength(SqlSimpleColumn column, int length, bool isMax)
        {
            string newLengthText =
                isMax ?
                ScriptFragmentGenerator.GenerateKeyword(CodeGenerationSupporter.Max) :
                length.ToString(CultureInfo.InvariantCulture);
            return SetColumnDataTypeLengthOrPrecision(column, newLengthText);
        }

        /// <summary>
        /// Sets the precision of the column's data type.
        /// </summary>
        /// <returns>The list of changes required to modify the data type</returns>
        public static IList<SqlScriptUpdateInfo> SetColumnDataTypePrecision(SqlSimpleColumn column, int precision)
        {
            return SetColumnDataTypeLengthOrPrecision(column, precision.ToString(CultureInfo.InvariantCulture));
        }

        public static IList<SqlScriptUpdateInfo> SetColumnDataTypeScale(SqlSimpleColumn column, int scale)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            SqlDataTypeReference dataTypeAst = (SqlDataTypeReference)columnAst.DataType;
            SqlBuiltInType columnDataType = (SqlBuiltInType)column.TypeSpecifier.Type;

            int startOffset = 0;
            int startLine = 0;
            int startColumn = 0;
            int fragmentLength = 0;
            string newText = scale.ToString(CultureInfo.InvariantCulture);
            if (dataTypeAst.Parameters.Count == 0)
            {
                // we don't have any parameter (no precision): insert precision when necessary and scale

                ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
                sfGen.AppendKeyword(TSqlTokenType.LeftParenthesis);

                if (SqlModelUpdaterConstants.SqlTypesCanHavePrecision.Contains(columnDataType.SqlDataType))
                {
                    // get the default precision
                    // column has a non-XML builtin type
                    string precisionText = ((SqlTypeSpecifier)column.TypeSpecifier).Precision.ToString(CultureInfo.InvariantCulture);

                    sfGen.AppendText(precisionText);
                    sfGen.AppendDelimiter(TSqlTokenType.Comma);
                }
                sfGen.AppendText(newText);
                sfGen.AppendKeyword(TSqlTokenType.RightParenthesis);
                newText = sfGen.GetScriptFragment();

                GetUpdateInfoForLengthPrecisionScale(dataTypeAst, out startOffset, out startLine, out startColumn, out fragmentLength);
            }
            else if (dataTypeAst.Parameters.Count == 1)
            {
                if (SqlModelUpdaterConstants.SqlTypesCanHavePrecision.Contains(columnDataType.SqlDataType))
                {
                    // the column data type can have precision, but no scale: insert comma and scale before closing parenthesis
                    int tokenIndex; // we actually don't use it
                    TSqlParserToken parenthesis;
                    SqlModelUpdaterUtils.FindToken(
                        columnAst.ScriptTokenStream,
                        dataTypeAst.Parameters[0].LastTokenIndex + 1,
                        token => token.TokenType == TSqlTokenType.RightParenthesis,
                        out tokenIndex,
                        out parenthesis);
                    if (parenthesis == null)
                    {
                        // no closing parenthesis?
                        SqlModelUpdaterUtils.TraceAndThrow("Can't find closing parenthesis for a data type with one parameter");
                    }

                    PopulatePositionInfoFromToken(parenthesis, out startOffset, out startLine, out startColumn, out fragmentLength);
                    newText = CodeGenerationSupporter.Comma + SqlModelUpdaterConstants.Space + newText;
                    fragmentLength = 0; // nothing to remove
                }
                else
                {
                    // the column data type can NOT have precision, so the existing parameter is scale: let's replace it
                    PopulatePositionInfoFromAst(dataTypeAst.Parameters[0], out startOffset, out startLine, out startColumn, out fragmentLength);
                }
            }
            else if (dataTypeAst.Parameters.Count == 2)
            {
                // we do have precision and scale, let's replace scale
                PopulatePositionInfoFromAst(dataTypeAst.Parameters[1], out startOffset, out startLine, out startColumn, out fragmentLength);
            }
            else
            {
                // more than two parameters? we don't really expect that
                SqlModelUpdaterUtils.TraceAndThrow("More than two parameters are found for a data type reference");
            }

            AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);

            return scriptUpdateList;
        }

        #endregion

        public IList<SqlScriptUpdateInfo> SetXmlColumnXmlSchema(SqlSimpleColumn column, SqlXmlSchemaCollection xmlSchema)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);

            int startOffset;
            int startLine;
            int startColumn;
            int fragmentLength;
            string newText;

            if (xmlSchema == null)
            {
                // remove schema and style: delete everything inside the parentheses and preceding and following space
                RemoveXmlSchema(columnAst, out startOffset, out startLine, out startColumn, out fragmentLength, out newText);
            }
            else
            {
                // replace exsiting schema or insert a new one
                ReplaceOrInsertXmlSchema(columnAst, xmlSchema, out startOffset, out startLine, out startColumn, out fragmentLength, out newText);
            }

            string cacheId = column.PrimarySource.SourceName;
            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(cacheId);
            scriptUpdate.AddUpdate(startOffset, startLine, startColumn, fragmentLength, newText);

            scriptUpdateList.Add(scriptUpdate);

            return scriptUpdateList;
        }

        internal static IList<SqlScriptUpdateInfo> SetCheckConstraintExpression(SqlCheckConstraint checkConstraint, string expression)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            if (!string.Equals(checkConstraint.CheckExpressionScript.Script, expression, StringComparison.Ordinal))
            {
                CheckConstraintDefinition checkConstraintAst = GetConstraintAst<CheckConstraintDefinition>(checkConstraint);

                if (checkConstraintAst == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find check constraint AST");
                }

                IList<TSqlParserToken> tokenStream = checkConstraintAst.ScriptTokenStream;
                BooleanExpression expressionAst = checkConstraintAst.CheckCondition;

                if (expressionAst == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find expression of a check constraint");
                }

                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, checkConstraint, expression, tokenStream[expressionAst.FirstTokenIndex], tokenStream[expressionAst.LastTokenIndex + 1]);
            }
            return scriptUpdateList;
        }

        internal static IList<SqlScriptUpdateInfo> SetIndexIsClustered(SqlIndex index, bool isClustered)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            CreateIndexStatement createIndexStatement = SqlModelUpdaterUtils.GetPrimaryAst<CreateIndexStatement>(index);
            IList<TSqlParserToken> tokenStream = createIndexStatement.ScriptTokenStream;

            if (createIndexStatement.Clustered.GetValueOrDefault(false) == isClustered)
            {
                return scriptUpdateList;
            }

            int foundTokenIndex;
            TSqlParserToken foundToken;

            // omit CREATE keyword in search (+1 to FirstIndexToken) as it is always in CREATE INDEX script
            SqlModelUpdaterUtils.FindToken(tokenStream, createIndexStatement.FirstTokenIndex + 1,
                token =>
                    token.TokenType == TSqlTokenType.Index ||
                    token.TokenType == TSqlTokenType.Clustered ||
                    token.TokenType == TSqlTokenType.NonClustered,
                out foundTokenIndex,
                out foundToken);

            if (foundToken == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot find INDEX/CLUSTERED/NONCLUSTERED keyword");
            }

            TSqlTokenType foundTokenType = foundToken.TokenType;

            if (isClustered)
            {
                if (foundTokenType == TSqlTokenType.Index)
                {
                    // CREATE [UNIQUE] INDEX -> CREATE [UNIQUE] CLUSTERED INDEX
                    string newScript = ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Clustered) + SqlModelUpdaterConstants.Space;
                    AddScriptUpdateForElementBeforeToken(scriptUpdateList, index, newScript, foundToken);
                }
                else if (foundTokenType == TSqlTokenType.NonClustered)
                {
                    // CREATE [UNIQUE] NONCLUSTERED INDEX -> CREATE [UNIQUE] CLUSTERED INDEX
                    string newScript = ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Clustered);
                    AddScriptUpdateForElementBetweenTokens(scriptUpdateList, index, newScript, foundToken, tokenStream[foundTokenIndex + 1]);
                }
            }
            else
            {
                if (foundTokenType == TSqlTokenType.Clustered)
                {
                    // CREATE [UNIQUE] CLUSTERED INDEX -> CREATE [UNIQUE] NONCLUSTERED INDEX
                    string newScript = ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.NonClustered);
                    AddScriptUpdateForElementBetweenTokens(scriptUpdateList, index, newScript, foundToken, tokenStream[foundTokenIndex + 1]);
                }
            }

            return scriptUpdateList;
        }

        internal static IList<SqlScriptUpdateInfo> SetIndexIsUnique(SqlIndex index, bool isUnique)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            CreateIndexStatement createIndexStatement = SqlModelUpdaterUtils.GetPrimaryAst<CreateIndexStatement>(index);
            IList<TSqlParserToken> tokenStream = createIndexStatement.ScriptTokenStream;

            if (createIndexStatement.Unique == isUnique)
            {
                return scriptUpdateList;
            }

            int foundTokenIndex;
            TSqlParserToken foundToken;

            // omit CREATE keyword in search (+1 to FirstIndexToken) as it is always in CREATE INDEX script
            SqlModelUpdaterUtils.FindToken(tokenStream, createIndexStatement.FirstTokenIndex + 1,
                token =>
                    token.TokenType == TSqlTokenType.Index ||
                    token.TokenType == TSqlTokenType.Clustered ||
                    token.TokenType == TSqlTokenType.NonClustered ||
                    token.TokenType == TSqlTokenType.Unique,
                out foundTokenIndex,
                out foundToken);

            if (foundToken == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot find INDEX/CLUSTERED/NONCLUSTERED/UNIQUE keyword");
            }

            if (isUnique)
            {
                // CREATE [CLUSTERED|NONCLUSTERED] INDEX -> CREATE UNIQUE [CLUSTERED|NONCLUSTERED] INDEX
                string newScript = ScriptFragmentGenerator.GenerateKeyword(TSqlTokenType.Unique) + SqlModelUpdaterConstants.Space;
                AddScriptUpdateForElementBeforeToken(scriptUpdateList, index, newScript, tokenStream[foundTokenIndex]);
            }
            else
            {
                Debug.Assert(foundToken.TokenType == TSqlTokenType.Unique);

                // CREATE UNIQUE [CLUSTERED|NONCLUSTERED] INDEX -> CREATE [CLUSTERED|NONCLUSTERED] INDEX
                int firstTokenToRemove = SqlModelUpdaterUtils.IsSpaceToken(tokenStream[foundTokenIndex - 1]) ? foundTokenIndex - 1 : foundTokenIndex;
                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, index, string.Empty, tokenStream[firstTokenToRemove], tokenStream[foundTokenIndex + 1]);
            }

            return scriptUpdateList;
        }

        public static IList<SqlScriptUpdateInfo> InsertFullTextIndexColumnAt(SqlFullTextIndex fullTextIndex, int index, SqlColumn column)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            CreateFullTextIndexStatement createStatement = SqlModelUpdaterUtils.GetPrimaryAst<CreateFullTextIndexStatement>(fullTextIndex);

            // Find the first token after table name
            int tokenIndex = createStatement.OnName.LastTokenIndex + 1;

            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(fullTextIndex.PrimarySource.SourceName);
            info.AddUpdate(InsertIntoDelimitedParenthesesWrappedList(
                     createStatement.ScriptTokenStream,
                     tokenIndex,
                     createStatement.FullTextIndexColumns.ToArray(),
                     index,
                     delegate(ScriptFragmentGenerator fragmentGenerator)
                     {
                         fragmentGenerator.AppendText(column.Name.Parts.Last());
                     }));
            scriptUpdates.Add(info);
            return scriptUpdates;
        }

        public static IList<SqlScriptUpdateInfo> UpdateIndexColumns(SqlIndex sqlIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            CreateIndexStatement createStatement = SqlModelUpdaterUtils.GetPrimaryAst<CreateIndexStatement>(sqlIndex);

            if (createStatement == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find AST for index");
            }

            return UpdateIndexOrKeyColumns(sqlIndex, createStatement.Columns, columns, sortOrder, createStatement.ScriptTokenStream);
        }

        public static IList<SqlScriptUpdateInfo> UpdateColumnStoreIndexColumns(SqlColumnStoreIndex sqlColumnStoreIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            CreateColumnStoreIndexStatement createStatement = SqlModelUpdaterUtils.GetPrimaryAst<CreateColumnStoreIndexStatement>(sqlColumnStoreIndex);

            if (createStatement == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find AST for columnstore index");
            }

            ColumnReferenceExpression firstEntry = createStatement.Columns.FirstOrDefault();
            ColumnReferenceExpression lastEntry = createStatement.Columns.LastOrDefault();

            if (firstEntry == null || lastEntry == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("index should have 1 column reference");
            }

            return InsertColumnTokensIntoParanthesis(firstEntry, lastEntry, sqlColumnStoreIndex, columns, sortOrder, createStatement.ScriptTokenStream);
        }

        private static IList<SqlScriptUpdateInfo> UpdateKeyWithImplicitColumn(SqlConstraint constraint, UniqueConstraintDefinition constraintAst, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            Debug.Assert(constraint is SqlPrimaryKeyConstraint || constraint is SqlUniqueConstraint);

            int foundTokenIndex;
            TSqlParserToken foundToken;
            int firstTokenToSearch = constraintAst.LastTokenIndex;
            IList<TSqlParserToken> tokenStream = constraintAst.ScriptTokenStream;

            SqlModelUpdaterUtils.FindTokenBackward(tokenStream,
                firstTokenToSearch,
                token =>
                    token.TokenType == TSqlTokenType.Key ||
                    token.TokenType == TSqlTokenType.Unique ||
                    token.TokenType == TSqlTokenType.Clustered ||
                    token.TokenType == TSqlTokenType.NonClustered,
                    out foundTokenIndex,
                    out foundToken);

            if (foundToken == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot find PRIMARY KEY/UNIQUE/CLUSTERED/NONCLUSTERED keyword");
            }

            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            // Insert string starting from begining of constraint to foundTokenIndex
            GetScriptFromTokens(sfGen, tokenStream, constraintAst.FirstTokenIndex, foundTokenIndex);

            // Insert columns string
            sfGen.AppendSpace();
            ScriptFragmentGenerator sfGenColList = GenerateColumnListInScriptFragmentGenerator(columns, sortOrder);
            sfGen.AppendText(sfGenColList.GetScriptFragment());

            // Insert string starting from foundTokenIndex + 1 to end of constraint
            if (foundTokenIndex < constraintAst.LastTokenIndex)
            {
                GetScriptFromTokens(sfGen, tokenStream, foundTokenIndex + 1, constraintAst.LastTokenIndex);
            }

            // Add table inline constraint
            // The caller will ensure DefiningTable is SqlTable
            SqlScriptUpdateInfo info = CreateScriptUpdateInfo((SqlTable)constraint.DefiningTable, sfGenConstraint => sfGenConstraint.AppendText(sfGen.GetScriptFragment()));

            // Delete original inline constraint
            info.AddUpdates(DeleteConstraintHelper(constraint));

            return new List<SqlScriptUpdateInfo> { info };
        }

        private static void GetScriptFromTokens(ScriptFragmentGenerator sfGen, IList<TSqlParserToken> tokenStream, int firstTokenIndex, int lastTokenIndex)
        {
            if (firstTokenIndex < 0 || firstTokenIndex > lastTokenIndex)
            {
                throw new ArgumentException("firstTokenIndex is invalid", "firstTokenIndex");
            }

            if (lastTokenIndex < 0 || lastTokenIndex > tokenStream.Count - 2)
            {
                throw new ArgumentException("lastTokenIndex is invalid", "lastTokenIndex");
            }

            for (int i = firstTokenIndex; i <= lastTokenIndex; i++)
            {
                sfGen.AppendText(tokenStream[i].Text);
            }
        }

        private static ScriptFragmentGenerator GenerateColumnListInScriptFragmentGenerator(IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            // replace old list of column references with the specified list
            IEnumerator<bool> sortOrderEnum = sortOrder.GetEnumerator();
            GenerateColumnList(sfGen, columns.ToList(), sfGenColList =>
            {
                if (!sortOrderEnum.MoveNext())
                {
                    SqlModelUpdaterUtils.TraceAndThrow("sortOrder should match entries in list of columns");
                }
                // append sort order after column name (ascending is the default)
                if (sortOrderEnum.Current == false)  // isAscending == false
                {
                    sfGenColList.AppendSpace();
                    sfGenColList.AppendKeyword(TSqlTokenType.Desc);
                }
            });

            if (sortOrderEnum.MoveNext())
            {
                SqlModelUpdaterUtils.TraceAndThrow("sortOrder should match entries in list of columns");
            }

            return sfGen;
        }

        private static IList<SqlScriptUpdateInfo> UpdateIndexOrKeyColumns(ISqlSpecifiesIndex indexOrKey, IList<ColumnWithSortOrder> oldColumnDefinitions,
                                                        IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder, IList<TSqlParserToken> tokenStream)
        {
            ColumnWithSortOrder firstEntry = oldColumnDefinitions.FirstOrDefault();
            ColumnWithSortOrder lastEntry = oldColumnDefinitions.LastOrDefault();

            if (firstEntry == null || lastEntry == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("index should have 1 column reference");
            }

            return InsertColumnTokensIntoParanthesis(firstEntry, lastEntry, indexOrKey, columns, sortOrder, tokenStream);
        }

        private static IList<SqlScriptUpdateInfo> InsertColumnTokensIntoParanthesis(TSqlFragment firstEntry, TSqlFragment lastEntry, ISqlModelElement statementModelElement,
                                                        IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder, IList<TSqlParserToken> tokenStream)
        {
            // get token indices for old list of column references
            int leftParenthesisIndex;
            int rightParenthesisIndex;
            GetParenthesesTokens(firstEntry, lastEntry, tokenStream, out leftParenthesisIndex, out rightParenthesisIndex);

            ScriptFragmentGenerator sfGen = GenerateColumnListInScriptFragmentGenerator(columns, sortOrder);

            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(statementModelElement.PrimarySource.SourceName);

            TSqlParserToken leftParens = tokenStream[leftParenthesisIndex];
            TSqlParserToken rightParens = tokenStream[rightParenthesisIndex];
            info.AddUpdate(new SqlScriptUpdateItem(
                leftParens.Offset,
                leftParens.Line,
                leftParens.Column,
                rightParens.Offset + rightParens.Text.Length - leftParens.Offset,
                sfGen.GetScriptFragment()));

            return new List<SqlScriptUpdateInfo> { info };

        }

        private static void GetParenthesesTokens(TSqlFragment firstFragment, TSqlFragment lastFragment, IList<TSqlParserToken> tokenStream, out int leftParenthesisIndex, out int rightParenthesisIndex)
        {
            TSqlParserToken leftParenthesisToken;
            TSqlParserToken rightParenthesisToken;

            SqlModelUpdaterUtils.FindTokenBackward(tokenStream, firstFragment.FirstTokenIndex - 1,
                token => token.TokenType == TSqlTokenType.LeftParenthesis, out leftParenthesisIndex, out leftParenthesisToken);
            SqlModelUpdaterUtils.FindToken(tokenStream, lastFragment.LastTokenIndex + 1,
                token => token.TokenType == TSqlTokenType.RightParenthesis, out rightParenthesisIndex, out rightParenthesisToken);

            if (leftParenthesisToken == null || rightParenthesisToken == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find parentheses of the column list");
            }
        }

        internal static IList<SqlScriptUpdateInfo> SetPrimaryKeyConstraintIsClustered(SqlPrimaryKeyConstraint primaryKeyConstraint, bool isClustered)
        {
            UniqueConstraintDefinition constraintDefinition = GetConstraintAst<UniqueConstraintDefinition>(primaryKeyConstraint);
            return SetPrimaryKeyOrUniqueConstraintIsClustered(primaryKeyConstraint, constraintDefinition, isClustered);
        }

        internal static IList<SqlScriptUpdateInfo> SetUniqueConstraintIsClustered(SqlUniqueConstraint uniqueConstraint, bool isClustered)
        {
            UniqueConstraintDefinition constraintDefinition = GetConstraintAst<UniqueConstraintDefinition>(uniqueConstraint);
            return SetPrimaryKeyOrUniqueConstraintIsClustered(uniqueConstraint, constraintDefinition, isClustered);
        }

        private static IList<SqlScriptUpdateInfo> SetPrimaryKeyOrUniqueConstraintIsClustered(ISqlModelElement constraint, UniqueConstraintDefinition constraintDefinition, bool isClustered)
        {
            Debug.Assert(constraint is SqlPrimaryKeyConstraint || constraint is SqlUniqueConstraint);
            if (constraintDefinition == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot find constraint AST");
            }

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            IList<TSqlParserToken> tokenStream = constraintDefinition.ScriptTokenStream;

            // If constraintDefinition.Clustered is not specified, we will always add explicit CLUSTERED or NONCLUSTERED keyword.
            if (constraintDefinition.Clustered.HasValue && constraintDefinition.Clustered.Value == isClustered)
            {
                return scriptUpdateList;
            }

            int foundTokenIndex;
            TSqlParserToken foundToken;
            int firstTokenToSearch = constraintDefinition.Columns.Count == 0 ? constraintDefinition.LastTokenIndex : constraintDefinition.Columns[0].FirstTokenIndex - 1;

            SqlModelUpdaterUtils.FindTokenBackward(tokenStream,
                firstTokenToSearch,
                token =>
                    token.TokenType == TSqlTokenType.Key ||
                    token.TokenType == TSqlTokenType.Unique ||
                    token.TokenType == TSqlTokenType.Clustered ||
                    token.TokenType == TSqlTokenType.NonClustered,
                    out foundTokenIndex,
                    out foundToken);

            if (foundToken == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot find PRIMARY KEY/UNIQUE/CLUSTERED/NONCLUSTERED keyword");
            }

            TSqlTokenType foundTokenType = foundToken.TokenType;
            string newKeyword = ScriptFragmentGenerator.GenerateKeyword(isClustered ? TSqlTokenType.Clustered : TSqlTokenType.NonClustered);

            if (foundTokenType == TSqlTokenType.Key || foundTokenType == TSqlTokenType.Unique)
            {
                // "[UNIQUE/PRIMARY KEY] (" -> "[UNIQUE/PRIMARY KEY] {CLUSTERED/NONCLUSTERED} ("
                string newScript = SqlModelUpdaterConstants.Space + newKeyword;
                AddScriptUpdateForElementBeforeToken(scriptUpdateList, constraint, newScript, tokenStream[foundTokenIndex + 1]);
            }
            else
            {
                Debug.Assert(
                    (foundTokenType == TSqlTokenType.NonClustered && isClustered) ||
                    (foundTokenType == TSqlTokenType.Clustered && !isClustered));
                // "[UNIQUE/PRIMARY KEY] {CLUSTERED/NONCLUSTERED} (" -> "[UNIQUE/PRIMARY KEY] {NONCLUSTERED/CLUSTERED} ("
                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, constraint, newKeyword, tokenStream[foundTokenIndex], tokenStream[foundTokenIndex + 1]);
            }

            return scriptUpdateList;
        }


        internal static IList<SqlScriptUpdateInfo> SetColumnExpressionScript(SqlComputedColumn column, string expression)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            if (!string.Equals(column.ExpressionScript.Script, expression, StringComparison.Ordinal))
            {
                ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
                IList<TSqlParserToken> tokenStream = columnAst.ScriptTokenStream;
                ScalarExpression expressionAst = columnAst.ComputedColumnExpression;

                if (expressionAst == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find expression of a computed column");
                }

                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, column, expression, tokenStream[expressionAst.FirstTokenIndex], tokenStream[expressionAst.LastTokenIndex + 1]);
            }
            return scriptUpdateList;
        }

        public static IList<SqlScriptUpdateInfo> SetColumnIsPersisted(SqlComputedColumn column, bool isPersisted)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);

            if (columnAst.IsPersisted == isPersisted)
            {
                return scriptUpdateList;
            }

            IList<TSqlParserToken> tokenStream = columnAst.ScriptTokenStream;

            if (isPersisted)
            {
                // "" -> "PERSISTED"
                // insert PERSISTED after computed expression
                AddScriptUpdateForElementBeforeToken(scriptUpdateList, column, SqlModelUpdaterConstants.Space + CodeGenerationSupporter.Persisted,
                    tokenStream[columnAst.ComputedColumnExpression.LastTokenIndex + 1]);
            }
            else
            {
                // "PERSISTED [NOT NULL]" -> ""
                Debug.Assert(columnAst.IsPersisted);

                int foundTokenIndex;
                TSqlParserToken foundToken;

                SqlModelUpdaterUtils.FindToken(tokenStream, columnAst.ComputedColumnExpression.LastTokenIndex + 1,
                    token => token.TokenType == TSqlTokenType.Identifier && token.Text.Equals(CodeGenerationSupporter.Persisted, StringComparison.OrdinalIgnoreCase),
                    out foundTokenIndex,
                    out foundToken);

                if (foundToken == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find PERSISTED keyword");
                }

                int firstTokenToRemove = SqlModelUpdaterUtils.IsSpaceToken(tokenStream[foundTokenIndex - 1]) ? foundTokenIndex - 1 : foundTokenIndex;
                int lastTokenToRemove = foundTokenIndex;

                int firstFoundTokenIndex;
                int lastFoundTokenIndex;

                bool found = SqlModelUpdaterUtils.CheckTokenSequence(tokenStream, foundTokenIndex + 1, columnAst.LastTokenIndex, out firstFoundTokenIndex, out lastFoundTokenIndex,
                    token => token.TokenType == TSqlTokenType.Not,
                    token => token.TokenType == TSqlTokenType.Null);

                if (found)
                {
                    // remove NOT NULL clause
                    lastTokenToRemove = lastFoundTokenIndex;
                }

                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, column, string.Empty, tokenStream[firstTokenToRemove], tokenStream[lastTokenToRemove + 1]);
            }

            return scriptUpdateList;
        }

        internal static IList<SqlScriptUpdateInfo> SetColumnIsPersistedNullable(SqlComputedColumn column, bool isPersistedNullable)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);

            // For the purposes of this particular comparison,
            // treat unspecified nullability of computed column the same as explicit nullability specified by isPersistedNullable parameter.
            bool columnNullability = column.IsPersistedNullable ?? true;
            if (columnNullability == isPersistedNullable)
            {
                return scriptUpdateList;
            }

            IList<TSqlParserToken> tokenStream = columnAst.ScriptTokenStream;

            if (isPersistedNullable)
            {
                // "PERSISTED NOT NULL" -> "PERSISTED"
                NullableConstraintDefinition nullableAst = null;

                foreach (var constraint in columnAst.Constraints)
                {
                    nullableAst = constraint as NullableConstraintDefinition;
                    if (nullableAst != null)
                    {
                        break;
                    }
                }

                if (nullableAst == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find NOT NULL keywords");
                }

                int firstToken = nullableAst.FirstTokenIndex;
                if (SqlModelUpdaterUtils.IsSpaceToken(tokenStream[firstToken - 1]))
                {
                    firstToken--;
                }
                AddScriptUpdateForElementBetweenTokens(scriptUpdateList, column, string.Empty, tokenStream[firstToken], tokenStream[nullableAst.LastTokenIndex + 1]);
            }
            else
            {
                // "PERSISTED" -> "PERSISTED NOT NULL"
                int foundTokenIndex;
                TSqlParserToken foundToken;

                SqlModelUpdaterUtils.FindToken(tokenStream, columnAst.ComputedColumnExpression.LastTokenIndex + 1,
                    token => token.TokenType == TSqlTokenType.Identifier && token.Text.Equals(CodeGenerationSupporter.Persisted, StringComparison.OrdinalIgnoreCase),
                    out foundTokenIndex, out foundToken);

                if (foundToken == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("cannot find PERSISTED keyword");
                }
                AddScriptUpdateForElementBeforeToken(scriptUpdateList, column, GenerateSpaceNotSpaceNull(), tokenStream[foundTokenIndex + 1]);
            }

            return scriptUpdateList;
        }

        private static void RemoveXmlSchema(
            ColumnDefinition columnAst,
            out int startOffset,
            out int startLine,
            out int startColumn,
            out int fragmentLength,
            out string newText)
        {
            int tokenIndex;
            TSqlParserToken firstToken;
            TSqlParserToken nextToken;

            // we have something like this: XML ( [DOCUMENT | CONTENT] schema_name )

            XmlDataTypeReference xmlDataTypeAst = (XmlDataTypeReference)columnAst.DataType;
            IList<TSqlParserToken> tokenStream = xmlDataTypeAst.ScriptTokenStream;

            // find the left parenthesis after XML
            TSqlParserToken leftParenthesisToken;
            SqlModelUpdaterUtils.FindToken(
                tokenStream,
                columnAst.DataType.Name.LastTokenIndex + 1,
                token => token.TokenType == TSqlTokenType.LeftParenthesis,
                out tokenIndex,
                out leftParenthesisToken);

            // if the preceding token is space, we remove it too
            if (SqlModelUpdaterUtils.IsSpaceToken(tokenStream[tokenIndex - 1]))
            {
                firstToken = tokenStream[tokenIndex - 1];
            }
            else
            {
                firstToken = leftParenthesisToken;
            }

            // find the right parenthesis after schema_name
            TSqlParserToken rightParenthesisToken;
            SqlModelUpdaterUtils.FindToken(
                tokenStream,
                xmlDataTypeAst.XmlSchemaCollection.LastTokenIndex + 1,
                token => token.TokenType == TSqlTokenType.RightParenthesis,
                out tokenIndex,
                out rightParenthesisToken);

            nextToken = tokenStream[tokenIndex + 1];

            PopulatePositionInfoFromToken(firstToken, out startOffset, out startLine, out startColumn, out fragmentLength);
            fragmentLength = nextToken.Offset - startOffset;
            newText = string.Empty;
        }

        private void ReplaceOrInsertXmlSchema(
            ColumnDefinition columnAst,
            SqlXmlSchemaCollection xmlSchema,
            out int startOffset,
            out int startLine,
            out int startColumn,
            out int fragmentLength,
            out string newText)
        {
            TSqlParserToken xmlToken;
            TSqlParserToken leftParenthesisToken;
            TSqlParserToken xmlStyleToken;
            TSqlParserToken xmlSchemaBeginToken;
            TSqlParserToken xmlSchemaEndToken;
            TSqlParserToken rightParenthesisToken;

            XmlDataTypeReference xmlDataTypeAst = (XmlDataTypeReference)columnAst.DataType;

            FindXmlDataTypeTokens(
                xmlDataTypeAst,
                out xmlToken,
                out leftParenthesisToken,
                out xmlStyleToken,
                out xmlSchemaBeginToken,
                out xmlSchemaEndToken,
                out rightParenthesisToken);

            if (xmlDataTypeAst.XmlSchemaCollection != null)
            {
                // replace existing XML schema
                PopulatePositionInfoFromToken(xmlSchemaBeginToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                fragmentLength = xmlSchemaEndToken.Offset + xmlSchemaEndToken.Text.Length - startOffset;
                SchemaObjectName xmlSchemaNameAst = ScriptDomUtils.CreateSchemaObjectName(xmlSchema.Name);
                _scriptGenerator.GenerateScript(xmlSchemaNameAst, out newText);
            }
            else
            {
                // insert XML schema
                startOffset = xmlToken.Offset + xmlToken.Text.Length;
                startLine = xmlToken.Line;
                startColumn = xmlToken.Column + xmlToken.Text.Length;
                fragmentLength = 0;
                SchemaObjectName xmlSchemaNameAst = ScriptDomUtils.CreateSchemaObjectName(xmlSchema.Name);
                string xmlSchemaName;
                _scriptGenerator.GenerateScript(xmlSchemaNameAst, out xmlSchemaName);
                newText = string.Format(
                    CultureInfo.InvariantCulture,
                    "({0})",
                    xmlSchemaName);
            }
        }

        public static IList<SqlScriptUpdateInfo> SetXmlColumnStyle(SqlSimpleColumn column, bool isXmlDocument)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            XmlDataTypeReference xmlDataTypeAst = (XmlDataTypeReference)columnAst.DataType;

            TSqlParserToken xmlTokn;
            TSqlParserToken leftParenthesisToken;
            TSqlParserToken xmlStyleToken;
            TSqlParserToken xmlSchemaBeginToken;
            TSqlParserToken xmlSchemaEndToken;
            TSqlParserToken rightParenthesisToken;
            FindXmlDataTypeTokens(
                xmlDataTypeAst,
                out xmlTokn,
                out leftParenthesisToken,
                out xmlStyleToken,
                out xmlSchemaBeginToken,
                out xmlSchemaEndToken,
                out rightParenthesisToken);

            string cacheId = column.PrimarySource.SourceName;
            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(cacheId);

            int startOffset;
            int startLine;
            int startColumn;
            int fragmentLength;
            string newText;
            if (isXmlDocument && xmlDataTypeAst.XmlDataTypeOption == XmlDataTypeOption.Content)
            {
                // change CONTENT to DOCUMENT
                PopulatePositionInfoFromToken(xmlStyleToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                newText = ScriptFragmentGenerator.GenerateKeyword(CodeGenerationSupporter.Document);
                scriptUpdate.AddUpdate(startOffset, startLine, startColumn, fragmentLength, newText);
            }
            else if (isXmlDocument && xmlDataTypeAst.XmlDataTypeOption == XmlDataTypeOption.None)
            {
                // insert "DOCUMENT " before XML schema
                PopulatePositionInfoFromToken(xmlSchemaBeginToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                fragmentLength = 0;
                newText = ScriptFragmentGenerator.GenerateKeyword(CodeGenerationSupporter.Document) + SqlModelUpdaterConstants.Space;
                scriptUpdate.AddUpdate(startOffset, startLine, startColumn, fragmentLength, newText);
            }
            else if (isXmlDocument == false && xmlDataTypeAst.XmlDataTypeOption == XmlDataTypeOption.Document)
            {
                // change DOCUMENT to CONTENT
                PopulatePositionInfoFromToken(xmlStyleToken, out startOffset, out startLine, out startColumn, out fragmentLength);
                newText = ScriptFragmentGenerator.GenerateKeyword(CodeGenerationSupporter.Content);
                scriptUpdate.AddUpdate(startOffset, startLine, startColumn, fragmentLength, newText);
            }

            if (scriptUpdate.Updates.Any())
            {
                scriptUpdateList.Add(scriptUpdate);
            }

            return scriptUpdateList;
        }


        private SqlScriptUpdateItem InsertColumnAtHelper(SqlTable table, int index, ScriptBuilderDelegate columnDefinitionBuilder)
        {
            CreateTableStatement tableAst = SqlModelUpdaterUtils.GetPrimaryAst<CreateTableStatement>(table);

            TSqlFragment[] collatedDefinitions = GetCollatedDefinitionsForTable(tableAst.Definition);

            // Graph edge tables may not have columns, which means the table definition is null.
            if(tableAst.Definition == null)
            {
                tableAst.Definition = new TableDefinition();
                tableAst.Definition.ScriptTokenStream = tableAst.ScriptTokenStream;
                tableAst.Definition.FirstTokenIndex = tableAst.SchemaObjectName.LastTokenIndex + 1;
            }

            return InsertIntoDelimitedParenthesesWrappedList(
                tableAst.Definition.ScriptTokenStream,
                tableAst.Definition.FirstTokenIndex,
                collatedDefinitions,
                GetCollatedIndexForDefinition<ColumnDefinition>(index, collatedDefinitions),
                columnDefinitionBuilder,
                multiline: true);
        }

        /// <summary>
        /// Returns a list with the column and constraint definitions in the order
        /// in which they are defined in the table definition
        /// </summary>
        internal static TSqlFragment[] GetCollatedDefinitionsForTable(TableDefinition tableDef)
        {
            SortedList<int, TSqlFragment> collatedDefinitions = new SortedList<int, TSqlFragment>();

            // Graph edge tables may not have columns, which means the table definition is null.
            if (tableDef != null)
            {
                foreach (ColumnDefinition colDef in tableDef.ColumnDefinitions)
                {
                    collatedDefinitions.Add(colDef.StartOffset, colDef);
                }
                foreach (ConstraintDefinition constrDef in tableDef.TableConstraints)
                {
                    collatedDefinitions.Add(constrDef.StartOffset, constrDef);
                }
            }

            return collatedDefinitions.Values.ToArray();
        }

        /// <summary>
        /// Converts an index that is relative only to definitions of the same kind (ColumnDefinitions vs. TableConstraints)
        /// into the index that is relative to the order in which all definitions are specified in the table
        /// </summary>
        internal static int GetCollatedIndexForDefinition<T>(int index, TSqlFragment[] collatedDefinitions) where T : TSqlFragment
        {
            int actualIndex = 0;

            while (actualIndex < collatedDefinitions.Length && !(collatedDefinitions[actualIndex] is T))
            {
                actualIndex++;
            }

            while (index > 0 && actualIndex < collatedDefinitions.Length)
            {
                if (collatedDefinitions[actualIndex] is T)
                {
                    index--;
                }
                actualIndex++;
            }

            return actualIndex;
        }

        #region Public Method that operate on Tables

        /// <summary>
        /// Generates the script changes required to insert a column set at the specified position.
        /// </summary>
        /// <returns>The list of changes required to insert the column</returns>
        public IList<SqlScriptUpdateInfo> InsertColumnSetAt(SqlTable table, string columnName, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(table.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            info.AddUpdate(InsertColumnAtHelper(
                     table,
                     index,
                     delegate(ScriptFragmentGenerator fragmentGenerator)
                     {
                         fragmentGenerator.AppendIdentifier(columnName);
                         fragmentGenerator.AppendSpace();
                         fragmentGenerator.AppendContextualKeyword(CodeGenerationSupporter.Xml);
                         fragmentGenerator.AppendSpace();
                         fragmentGenerator.AppendContextualKeyword(CodeGenerationSupporter.ColumnSet);
                         fragmentGenerator.AppendSpace();
                         fragmentGenerator.AppendKeyword(TSqlTokenType.For);
                         fragmentGenerator.AppendSpace();
                         fragmentGenerator.AppendContextualKeyword(CodeGenerationSupporter.AllSparseColumns);
                     }));
            return scriptUpdates;
        }

        /// <summary>
        /// Generates the script changes required to insert a computed column at the specified position.
        /// </summary>
        /// <returns>The list of changes required to insert the column</returns>
        public IList<SqlScriptUpdateInfo> InsertComputedColumnAt(SqlTable table, string columnName, int index, string expression)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(table.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            info.AddUpdate(InsertColumnAtHelper(
                table,
                index,
                delegate(ScriptFragmentGenerator fragmentGenerator)
                {
                    fragmentGenerator.AppendIdentifier(columnName);
                    fragmentGenerator.AppendSpace();
                    fragmentGenerator.AppendKeyword(TSqlTokenType.As);
                    fragmentGenerator.AppendSpace();
                    fragmentGenerator.AppendText(expression);
                }));
            return scriptUpdates;
        }

        /// <summary>
        /// Generates the script changes required to insert a simple column at the specified position.
        /// </summary>
        /// <returns>The list of changes required to insert the column</returns>
        public IList<SqlScriptUpdateInfo> InsertSimpleColumnAt(SqlTable table, string columnName, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(table.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            info.AddUpdate(InsertColumnAtHelper(
                table,
                index,
                delegate(ScriptFragmentGenerator fragmentGenerator)
                {
                    fragmentGenerator.AppendIdentifier(columnName);
                    fragmentGenerator.AppendSpace();
                    fragmentGenerator.AppendContextualKeyword(CodeGenerationSupporter.NChar);
                    fragmentGenerator.AppendKeyword(TSqlTokenType.LeftParenthesis);
                    fragmentGenerator.AppendText(SqlModelUpdaterConstants.DefaultNCharLength.ToString(CultureInfo.InvariantCulture));
                    fragmentGenerator.AppendKeyword(TSqlTokenType.RightParenthesis);
                    fragmentGenerator.AppendSpace();
                    fragmentGenerator.AppendKeyword(TSqlTokenType.Null);
                }));
            return scriptUpdates;
        }
        #endregion


        public IList<SqlScriptUpdateInfo> AddInlineIndex(SqlTable table, string inlineIndexName)
        {
            return new List<SqlScriptUpdateInfo>() { CreateScriptUpdateInfo(table, sfGen => GenerateInlineIndexScript(sfGen, inlineIndexName)) };
        }

        private static void GenerateInlineIndexScript(ScriptFragmentGenerator sfGen, string inlineIndexName)
        {
            const string inlineIndex = "INDEX {0} NONCLUSTERED HASH ([Column]) WITH (BUCKET_COUNT = 131072)";
            sfGen.AppendText(string.Format(CultureInfo.InvariantCulture,
                inlineIndex,
                Identifier.EncodeIdentifier(inlineIndexName)
            ));
        }

        public IList<SqlScriptUpdateInfo> AddCheckConstraint(SqlTable table, string constraintName)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen => GenerateCheckConstraintScript(sfGen, constraintName)) };
        }

        private void GenerateCheckConstraintScript(ScriptFragmentGenerator sfGen, string constraintName)
        {
            const string constraint = "CONSTRAINT {0} CHECK (1 = 1)";
            sfGen.AppendText(string.Format(CultureInfo.InvariantCulture, constraint, Identifier.EncodeIdentifier(constraintName)));
        }

        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, string constraintName)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen =>
                {
                    if (table.IsMemoryOptimized)
                    {
                        GenerateHekatonPrimaryKeyConstraintScript(sfGen, constraintName,null);
                    }
                    else
                    {
                        GeneratePrimaryKeyConstraintScript(sfGen, constraintName);
                    }
                }
                ) };
        }

        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, IEnumerable<SqlColumn> columns)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen =>
                {
                    if (table.IsMemoryOptimized)
                    {
                        GenerateHekatonPrimaryKeyConstraintScript(sfGen,null,columns);
                    }
                    else
                    {
                        GeneratePrimaryKeyConstraintScript(sfGen, null, columns);
                    }
                }
                ) };
        }

        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> columns)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen =>
                {
                    if (table.IsMemoryOptimized)
                    {
                        GenerateHekatonPrimaryKeyConstraintScript(sfGen,constraintName,columns);
                    }
                    else
                    {
                        GeneratePrimaryKeyConstraintScript(sfGen, constraintName, columns);
                    }
                }) };
        }

        private static void GeneratePrimaryKeyScriptBase(ScriptFragmentGenerator sfGen, string constraintName)
        {
            // generate: CONSTRAINT name PRIMARY KEY

            if (!string.IsNullOrWhiteSpace(constraintName))
            {
                sfGen.AppendKeyword(TSqlTokenType.Constraint);
                sfGen.AppendSpace();
                sfGen.AppendIdentifier(constraintName); // we always encode the identifier with brackets, TODO: encode when only necessary 
                sfGen.AppendSpace();
            }

            sfGen.AppendKeyword(TSqlTokenType.Primary);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Key);
        }

        private void GenerateHekatonPrimaryKeyConstraintScript(ScriptFragmentGenerator sfGen, string constraintName, IEnumerable<SqlColumn> columns)
        {
            // Generate: CONSTRAINT [name] PRIMARY KEY NONCLUSTERED HASH ([column]) WITH (BUCKET_COUNT = 131072)
            const string hash = "HASH";
            const string bucketCount = " WITH (BUCKET_COUNT = 131072)";
            GeneratePrimaryKeyScriptBase(sfGen, constraintName);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.NonClustered);
            sfGen.AppendSpace();
            sfGen.AppendText(hash);
            sfGen.AppendSpace();
            if (columns == null || columns.Count() == 0)
            {
                AppendCollectionWithPlaceholderColumn(sfGen);
            }
            else
            {
                GenerateColumnList(sfGen, columns.ToList());
            }
            sfGen.AppendText(bucketCount);
        }

        private void GeneratePrimaryKeyConstraintScript(ScriptFragmentGenerator sfGen, string constraintName)
        {
            // generate: CONSTRAINT name PRIMARY KEY ([Column])

            GeneratePrimaryKeyScriptBase(sfGen, constraintName);
            sfGen.AppendSpace();
            AppendCollectionWithPlaceholderColumn(sfGen);
        }

        private void GeneratePrimaryKeyConstraintScript(ScriptFragmentGenerator sfGen, string constraintName, IEnumerable<SqlColumn> columns)
        {
            // generate: CONSTRAINT name PRIMARY KEY (columns...)

            GeneratePrimaryKeyScriptBase(sfGen, constraintName);
            sfGen.AppendSpace();
            GenerateColumnList(sfGen, columns.ToList());
        }

        public static IList<SqlScriptUpdateInfo> UpdatePrimaryKeyColumns(SqlPrimaryKeyConstraint primaryKeyConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            UniqueConstraintDefinition constraintAst = GetConstraintAst<UniqueConstraintDefinition>(primaryKeyConstraint);

            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find AST for primary key");
            }

            if (constraintAst.Columns == null || constraintAst.Columns.Count == 0)
            {
                return UpdateKeyWithImplicitColumn(primaryKeyConstraint, constraintAst, columns, sortOrder);
            }
            else
            {
                return UpdateIndexOrKeyColumns(primaryKeyConstraint, constraintAst.Columns, columns, sortOrder, constraintAst.ScriptTokenStream);
            }
        }

        public IList<SqlScriptUpdateInfo> AddUniqueConstraint(SqlTable table, string constraintName)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen => GenerateUniqueConstraintScript(sfGen, constraintName)) };
        }

        private void GenerateUniqueConstraintScript(ScriptFragmentGenerator sfGen, string constraintName)
        {
            // generate: CONSTRAINT name UNIQUE ([Column])

            sfGen.AppendKeyword(TSqlTokenType.Constraint);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(constraintName);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Unique);
            sfGen.AppendSpace();
            AppendCollectionWithPlaceholderColumn(sfGen);
        }

        public static IList<SqlScriptUpdateInfo> UpdateUniqueConstraintColumns(SqlUniqueConstraint uniqueConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            UniqueConstraintDefinition constraintAst = GetConstraintAst<UniqueConstraintDefinition>(uniqueConstraint);

            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find AST for unique constraint");
            }

            if (constraintAst.Columns == null || constraintAst.Columns.Count == 0)
            {
                return UpdateKeyWithImplicitColumn(uniqueConstraint, constraintAst, columns, sortOrder);
            }
            else
            {
                return UpdateIndexOrKeyColumns(uniqueConstraint, constraintAst.Columns, columns, sortOrder, constraintAst.ScriptTokenStream);
            }
        }

        public IList<SqlScriptUpdateInfo> AddForeignKeyConstraint(SqlTable table, string constraintName)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen => GenerateForeignKeyScript(sfGen, constraintName)) };
        }

        internal IList<SqlScriptUpdateInfo> AddForeignKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> referencingColumns, SqlTableBase referencedTable, IEnumerable<SqlColumn> referencedColumns)
        {
            return new List<SqlScriptUpdateInfo> { CreateScriptUpdateInfo(table, sfGen => GenerateForeignKeyScript(sfGen, constraintName, referencingColumns, referencedTable, referencedColumns)) };
        }

        private void GenerateForeignKeyScriptBase(ScriptFragmentGenerator sfGen, string constraintName)
        {
            // generate: CONSTRAINT name FOREIGN KEY
            sfGen.AppendKeyword(TSqlTokenType.Constraint);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(constraintName);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Foreign);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Key);
        }

        private void GenerateForeignKeyScript(ScriptFragmentGenerator sfGen, string constraintName)
        {
            // generate: CONSTRAINT name FOREIGN KEY ([Column]) REFERENCES [ToTable]([ToTableColumn])
            GenerateForeignKeyScriptBase(sfGen, constraintName);
            sfGen.AppendSpace();
            AppendCollectionWithPlaceholderColumn(sfGen);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.References);
            sfGen.AppendSpace();
            // add a placeholder entry for referenced table so the statement can be parsed
            sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderForeignTableName);
            AppendCollectionWithPlaceholderColumn(sfGen, SqlModelUpdaterConstants.PlaceholderForeignColumnName);
        }

        private void GenerateForeignKeyScript(ScriptFragmentGenerator sfGen, string constraintName, IEnumerable<SqlColumn> referencingColumns, SqlTableBase referencedTable, IEnumerable<SqlColumn> referencedColumns)
        {
            // generate: CONSTRAINT name FOREIGN KEY ([referencingColumns]) REFERENCES [referencedTable] (referencedColumns])
            GenerateForeignKeyScriptBase(sfGen, constraintName);
            sfGen.AppendSpace();
            GenerateColumnList(sfGen, referencingColumns.ToList());
            sfGen.AppendKeyword(TSqlTokenType.References);
            sfGen.AppendSpace();
            sfGen.AppendText(GenerateSchemaQualifiedIdentifier(referencedTable));
            GenerateColumnList(sfGen, referencedColumns.ToList());
        }

        public IList<SqlScriptUpdateInfo> AddDefaultConstraint(SqlSimpleColumn column, string expression)
        {
            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            TSqlParserToken nextToken = columnAst.ScriptTokenStream[columnAst.LastTokenIndex + 1];

            string defaultConstraintScript = GenerateDefaultConstraintScript(expression);

            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(column.PrimarySource.SourceName);
            scriptUpdate.AddUpdate(nextToken.Offset, nextToken.Line, nextToken.Column, 0, defaultConstraintScript);
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(scriptUpdate);
            return scriptUpdateList;
        }

        private string GenerateDefaultConstraintScript(string expression)
        {
            // generate: DEFAULT expression

            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Default);
            sfGen.AppendSpace();
            sfGen.AppendText(expression);

            return sfGen.GetScriptFragment();
        }

        private static SqlScriptUpdateInfo CreateScriptUpdateInfo(SqlTable table, ScriptBuilderDelegate constraintDefBuilder)
        {
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(table.PrimarySource.SourceName);

            TableDefinition tableDef = SqlModelUpdaterUtils.GetPrimaryAst<CreateTableStatement>(table).Definition;

            TSqlFragment[] collatedDefinitions = GetCollatedDefinitionsForTable(tableDef);

            info.AddUpdate(InsertIntoDelimitedParenthesesWrappedList(
                tableDef.ScriptTokenStream,
                tableDef.FirstTokenIndex,
                collatedDefinitions,
                collatedDefinitions.Length, // insert at the end of the list
                constraintDefBuilder,
                multiline: true));

            return info;
        }

        public IList<SqlScriptUpdateInfo> SetDefaultConstraintExpression(SqlDefaultConstraint defaultConstraint, string expression)
        {
            List<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            if (!string.Equals(defaultConstraint.DefaultExpressionScript.Script, expression, StringComparison.Ordinal))
            {
                DefaultConstraintDefinition constraintAst = GetConstraintAst<DefaultConstraintDefinition>(defaultConstraint);
                ScalarExpression expressionAst = constraintAst.Expression;

                IList<TSqlParserToken> tokenStream = expressionAst.ScriptTokenStream;
                TSqlParserToken previousToken = tokenStream[expressionAst.FirstTokenIndex - 1];
                if (SqlModelUpdaterUtils.IsSignificantToken(previousToken))
                {
                    ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
                    sfGen.AppendSpace();
                    sfGen.AppendText(expression);
                    expression = sfGen.GetScriptFragment();
                }

                AddScriptUpdateForElement(scriptUpdateList, defaultConstraint, expression, expressionAst.StartOffset, expressionAst.StartLine, expressionAst.StartColumn, expressionAst.FragmentLength);
            }
            return scriptUpdateList;
        }

        public IList<SqlScriptUpdateInfo> CreateIndex(SqlTable table, string indexName)
        {
            string createIndexScript = GenerateCreateIndexStatement(table, indexName);

            // If we're creating an index on a temporal system-versioned history table
            // note that this gets scripted along with temporal 'current' table DDL and indexes
            // as we don't script history table explicitly
            //
            SqlTable targetTable = table.TemporalSystemVersioningCurrentTable != null ? table.TemporalSystemVersioningCurrentTable : table;

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(targetTable, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateIndexStatement(SqlTable table, string indexName)
        {
            // generate: 
            // 
            // CREATE INDEX indexName ON [schema].[table] ([ ])
            //
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(indexName);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateXmlIndex(SqlTable table, string indexName, bool isPrimary)
        {
            string createIndexScript = GenerateCreateXmlIndexStatement(table, indexName, isPrimary);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateXmlIndexStatement(SqlTable table, string indexName, bool isPrimary)
        {
            // generate: 
            // 
            // CREATE PRIMARY XML INDEX indexName ON [schema].[table] ([ ])
            // OR 
            /// CREATE XML INDEX indexName ON [schema].[table] ([ ]) USING XML INDEX primaryIndexName

            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            if (isPrimary)
            {
                sfGen.AppendKeyword(TSqlTokenType.Primary);
                sfGen.AppendSpace();
            }
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.Xml);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(indexName);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            if (!isPrimary)
            {
                sfGen.AppendSpace();
                sfGen.AppendContextualKeyword(CodeGenerationSupporter.Using);
                sfGen.AppendSpace();
                sfGen.AppendContextualKeyword(CodeGenerationSupporter.Xml);
                sfGen.AppendSpace();
                sfGen.AppendKeyword(TSqlTokenType.Index);
                sfGen.AppendSpace();
                sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderXmlIndexName);
                sfGen.AppendSpace();
                sfGen.AppendContextualKeyword(SqlModelUpdaterConstants.FOR);
                sfGen.AppendSpace();
                sfGen.AppendContextualKeyword(SqlModelUpdaterConstants.PATH);
            }
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateSelectiveXmlIndex(SqlTable table, string indexName, bool isPrimary)
        {
            string createIndexScript = GenerateCreateSelectiveXmlIndexStatement(table, indexName, isPrimary);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateSelectiveXmlIndexStatement(SqlTable table, string indexName, bool isPrimary)
        {
            // generate: 
            // 
            // CREATE SELECTIVE XML INDEX indexName ON [schema].[table] ([ ]) FOR (Identifier='LiteralValue')
            //
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            if (isPrimary)
            {
                // Only primary selective indexes specify the keyword SELECTIVE
                sfGen.AppendContextualKeyword(SqlModelUpdaterConstants.SELECTIVE);
                sfGen.AppendSpace();
            }
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.Xml);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(indexName);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            sfGen.AppendSpace();

            if (!isPrimary)
            {
                // Append USING XML INDEX [PlaceHolderName]
                sfGen.AppendContextualKeyword(CodeGenerationSupporter.Using);
                sfGen.AppendSpace();
                sfGen.AppendContextualKeyword(CodeGenerationSupporter.Xml);
                sfGen.AppendSpace();
                sfGen.AppendKeyword(TSqlTokenType.Index);
                sfGen.AppendSpace();
                sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderSelectiveXmlIndexName);
                sfGen.AppendSpace();
            }

            sfGen.AppendKeyword(TSqlTokenType.For);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.LeftParenthesis);
            if (isPrimary)
            {
                sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderXPathName);
                sfGen.AppendKeyword(TSqlTokenType.EqualsSign);
                sfGen.AppendText(GetStringForStringLiteral(SqlModelUpdaterConstants.PlaceholderXPathLiteral));
            }
            else
            {
                sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderXPathName);
            }
            sfGen.AppendKeyword(TSqlTokenType.RightParenthesis);
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateSpatialIndex(SqlTable table, string indexName)
        {
            string createIndexScript = GenerateCreateSpatialIndexStatement(table, indexName);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateSpatialIndexStatement(SqlTable table, string indexName)
        {
            // generate: 
            // 
            // CREATE SPATIAL INDEX indexName ON [schema].[table] ([ ])
            //
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.Spatial);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(indexName);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateFullTextIndex(SqlTable table)
        {
            string createIndexScript = GenerateCreateFullTextIndex(table);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateFullTextIndex(SqlTable table)
        {
            // generate: 
            // 
            // CREATE FULLTEXT INDEX ON [dbo].[TableFoo] (Id) KEY INDEX [unique_index_name]	ON [fulltext_catalog_name] WITH CHANGE_TRACKING AUTO
            //
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.Fulltext);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Key);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderUniqueIndexName);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.On);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(SqlModelUpdaterConstants.PlaceholderCatalogName);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.With);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.ChangeTracking);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(CodeGenerationSupporter.Auto);
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateColumnStoreIndex(SqlTable table, string indexName)
        {
            string createIndexScript = GenerateCreateColumnStoreIndexStatement(table, indexName);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createIndexScript));
            return scriptUpdateList;
        }

        private string GenerateCreateColumnStoreIndexStatement(SqlTable table, string indexName)
        {
            // generate: 
            // 
            // CREATE COLUMNSTORE INDEX indexName ON [schema].[table] ([ ])
            //
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(SqlModelUpdaterConstants.COLUMNSTORE);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Index);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(indexName);
            sfGen.AppendSpace();
            GenerateOnTableSpecForIndex(table, sfGen);
            sfGen.AppendNewLine();

            return sfGen.GetScriptFragment();
        }

        public IList<SqlScriptUpdateInfo> CreateDmlTrigger(SqlTable table, string dmlTriggerName)
        {
            string createDmlTriggerStatement = GenerateDmlTriggerStatement(table, dmlTriggerName);

            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(table, createDmlTriggerStatement));

            return scriptUpdateList;
        }

        private string GenerateDmlTriggerStatement(SqlTable table, string dmlTriggerName)
        {
            /// Generate:
            /// CREATE TRIGGER [schemaname].[triggername]
            ///     ON [schemaname].[tablename]
            ///     FOR DELETE, INSERT, UPDATE
            ///     AS
            ///     BEGIN
            ///         SET NoCount ON
            ///     END
            ///
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendNewLine();
            sfGen.AppendKeyword(TSqlTokenType.Create);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Trigger);
            sfGen.AppendSpace();
            GenerateTriggerNameWithSchema(table, dmlTriggerName, sfGen);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            GenerateOnTableSpec(table, sfGen);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            sfGen.AppendKeyword(TSqlTokenType.For);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Delete);
            sfGen.AppendKeyword(TSqlTokenType.Comma);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Insert);
            sfGen.AppendKeyword(TSqlTokenType.Comma);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Update);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            sfGen.AppendKeyword(TSqlTokenType.As);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            sfGen.AppendKeyword(TSqlTokenType.Begin);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            sfGen.AppendSpace(4);
            sfGen.AppendKeyword(TSqlTokenType.Set);
            sfGen.AppendSpace();
            sfGen.AppendContextualKeyword(SetOptions.NoCount.ToString());
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.On);
            sfGen.AppendNewLine();
            sfGen.AppendSpace(4);
            sfGen.AppendKeyword(TSqlTokenType.End);

            return sfGen.GetScriptFragment();
        }

        private void GenerateOnTableSpecForIndex(SqlTable table, ScriptFragmentGenerator sfGen)
        {
            // generate: ON [schema].[table] ([Column])

            GenerateOnTableSpec(table, sfGen);
            sfGen.AppendSpace();
            AppendCollectionWithPlaceholderColumn(sfGen);
        }

        private void GenerateOnTableSpec(SqlTable table, ScriptFragmentGenerator sfGen)
        {
            sfGen.AppendKeyword(TSqlTokenType.On);
            sfGen.AppendSpace();
            sfGen.AppendText(GenerateSchemaQualifiedIdentifier(table));
        }

        private void GenerateTriggerNameWithSchema(SqlTable table, string dmlTriggerName, ScriptFragmentGenerator sfGen)
        {
            // generate: [tableSchemaName].[dmlTriggerName]

            string triggerSchemaName = table.Schema.Name.Parts[0];
            string[] nameParts = new string[2] { triggerSchemaName, dmlTriggerName };
            SchemaObjectName schemaObjectName = ScriptDomUtils.CreateSchemaObjectName(nameParts);

            string script;
            _scriptGenerator.GenerateScript(schemaObjectName, out script);
            sfGen.AppendText(script);
        }

        ///// <summary>
        ///// Generates the script changes required to insert an element into a collection at the specified position.
        ///// </summary>
        /// <param name="tokenStream">The stream of tokens representing the statement</param>
        /// <param name="nextTokenIndex">The token representing the position where the collection should be created if one doesn't exist already.</param>
        /// <param name="collection">An array representing the TSqlFragments that make up the collection</param>
        /// <param name="collatedCollectionCount">In cases where the collection can be collated, the total count of elements</param>
        /// <param name="index">Position where the element should be inserted.</param>
        /// <param name="elementBuilder">Delegate that gets called when the actual script needs to be generated</param>
        ///// <returns>The list of changes required to insert the column</returns>
        private static SqlScriptUpdateItem InsertIntoDelimitedParenthesesWrappedList(IList<TSqlParserToken> tokenStream, int nextTokenIndex, TSqlFragment[] collection, int index, ScriptBuilderDelegate elementBuilder, bool multiline = false)
        {
            // Construct the script to be inserted
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            SqlModelUpdaterUtils.InsertIntoCollection(sfGen, index, collection.Length, elementBuilder, multiline);

            // locate the inserting position
            TSqlParserToken nextToken = null;

            if (index == collection.Length)
            {
                if (index != 0)
                {
                    // insert a new column/constraint after the last entry
                    SqlModelUpdaterUtils.FindToken(
                        tokenStream,
                        collection[collection.Length - 1].LastTokenIndex + 1,
                        token => token.TokenType == TSqlTokenType.RightParenthesis || token.TokenType == TSqlTokenType.Comma,
                        out nextTokenIndex,
                        out nextToken);

                    // insert before any whitespace tokens
                    SqlModelUpdaterUtils.FindTokenBackward(
                        tokenStream,
                        nextTokenIndex - 1,
                        token => token.TokenType != TSqlTokenType.WhiteSpace,
                        out nextTokenIndex,
                        out nextToken);
                    if (nextToken.TokenType == TSqlTokenType.SingleLineComment)
                    {
                        SqlModelUpdaterUtils.FindToken(
                            tokenStream,
                            nextTokenIndex,
                            token => SqlModelUpdaterUtils.IsNewLineToken(token),
                            out nextTokenIndex,
                            out nextToken);
                    }
                    nextTokenIndex++;
                }
            }
            else // model updater has validated that index is not greater than column + constraint count
            {
                SqlModelUpdaterUtils.FindTokenBackward(
                    tokenStream,
                    collection[index].FirstTokenIndex - 1,
                    token => token.TokenType == TSqlTokenType.LeftParenthesis || token.TokenType == TSqlTokenType.Comma,
                    out nextTokenIndex,
                    out nextToken);
                nextTokenIndex++;

                // insert after any whitespace tokens
                SqlModelUpdaterUtils.FindToken(
                    tokenStream,
                    nextTokenIndex,
                    token => token.TokenType != TSqlTokenType.WhiteSpace,
                    out nextTokenIndex,
                    out nextToken);
            }

            nextToken = tokenStream[nextTokenIndex];

            SqlScriptUpdateItem item = new SqlScriptUpdateItem(
                nextToken.Offset,
                nextToken.Line,
                nextToken.Column,
                length: 0, // nothing to delete
                newText: sfGen.GetScriptFragment());

            return item;
        }

        public IList<SqlScriptUpdateInfo> DeleteColumn(SqlColumn column)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo sqlScriptUpdateInfo = new SqlScriptUpdateInfo(column.PrimarySource.SourceName);

            sqlScriptUpdateInfo.AddUpdates(SqlScriptUpdaterForTableElementDeletion.DeleteColumn(column));

            scriptUpdateList.Add(sqlScriptUpdateInfo);

            return scriptUpdateList;
        }

        public IList<SqlScriptUpdateInfo> DeleteIndex(ISqlModelElement index)
        {
            SqlIndex sqlIndex = index as SqlIndex;
            if (sqlIndex != null && index.GetAnnotations<SqlInlineIndexAnnotation>().Count > 0)
            {
                SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(sqlIndex.PrimarySource.SourceName);
                TSqlFragment fragment = SqlModelUpdaterUtils.GetPrimaryAst<TSqlFragment>(sqlIndex);
                var indexAst = fragment as IndexDefinition;
                if (indexAst != null)
                {
                    info.AddUpdates(SqlScriptUpdaterForTableElementDeletion.DeleteInlineIndex(sqlIndex));
                }
                else
                {
                    info.AddUpdate(DeleteModelElementPrimarySource(sqlIndex));
                }
                return new List<SqlScriptUpdateInfo> { info };
            }

            return DeleteCreateAndAlterStatementsForElement(index);
        }

        public IList<SqlScriptUpdateInfo> DeleteTrigger(SqlDmlTrigger trigger)
        {
            return DeleteCreateAndAlterStatementsForElement(trigger);
        }

        private static void AppendCollectionWithPlaceholderColumn(ScriptFragmentGenerator sfGen, string placeholderName = SqlModelUpdaterConstants.PlaceholderColumnName)
        {
            // add a placeholder column entry since parser does not allow empty collection
            SqlModelUpdaterUtils.InsertIntoCollection(sfGen, 0, 0, (ScriptFragmentGenerator) =>
            {
                sfGen.AppendIdentifier(placeholderName, encode: true);
            });
        }

        private string GenerateSchemaQualifiedIdentifier(IModelElement element)
        {
            SchemaObjectName schemaObjectName = ScriptDomUtils.CreateSchemaObjectName(element.Name);

            string script;
            _scriptGenerator.GenerateScript(schemaObjectName, out script);
            return script;
        }

        private static void FindXmlDataTypeTokens(
            XmlDataTypeReference xmlDataTypeAst,
            out TSqlParserToken xmlToken,
            out TSqlParserToken leftParenthesisToken,
            out TSqlParserToken xmlStyleToken,
            out TSqlParserToken xmlSchemaBeginToken,
            out TSqlParserToken xmlSchemaEndToken,
            out TSqlParserToken rightParenthesisToken)
        {
            xmlToken = null;
            leftParenthesisToken = null;
            xmlStyleToken = null;
            xmlSchemaBeginToken = null;
            xmlSchemaEndToken = null;
            rightParenthesisToken = null;

            IList<TSqlParserToken> tokenStream = xmlDataTypeAst.ScriptTokenStream;
            xmlToken = tokenStream[xmlDataTypeAst.Name.FirstTokenIndex];
            if (xmlDataTypeAst.XmlSchemaCollection != null)
            {
                int tokenIndex;
                SqlModelUpdaterUtils.FindToken(
                    tokenStream,
                    xmlDataTypeAst.Name.FirstTokenIndex + 1,
                    token => token.TokenType == TSqlTokenType.LeftParenthesis,
                    out tokenIndex,
                    out leftParenthesisToken);
                if (xmlDataTypeAst.XmlDataTypeOption != XmlDataTypeOption.None)
                {
                    SqlModelUpdaterUtils.FindToken(
                        tokenStream,
                        tokenIndex + 1,
                        token => token.TokenType == TSqlTokenType.Identifier, // CONTENT or DOCUMENT
                        out tokenIndex,
                        out xmlStyleToken);
                }
                xmlSchemaBeginToken = tokenStream[xmlDataTypeAst.XmlSchemaCollection.FirstTokenIndex];
                xmlSchemaEndToken = tokenStream[xmlDataTypeAst.XmlSchemaCollection.LastTokenIndex];
                SqlModelUpdaterUtils.FindToken(
                    tokenStream,
                    xmlDataTypeAst.XmlSchemaCollection.LastTokenIndex + 1,
                    token => token.TokenType == TSqlTokenType.RightParenthesis,
                    out tokenIndex,
                    out rightParenthesisToken);
            }
        }

        private static IList<SqlScriptUpdateInfo> SetColumnDataTypeLengthOrPrecision(SqlSimpleColumn column, string lengthOrPrecision)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            SqlDataTypeReference dataTypeAst = (SqlDataTypeReference)columnAst.DataType;

            int startOffset = 0;
            int startLine = 0;
            int startColumn = 0;
            int fragmentLength = 0;
            string newText = lengthOrPrecision;
            if (dataTypeAst.Parameters.Count == 0)
            {
                // we don't have any parameter (no length or precision), let's add one

                // wrap the length with parenthesis
                newText = string.Format(CultureInfo.InvariantCulture, "({0})", newText);

                GetUpdateInfoForLengthPrecisionScale(dataTypeAst, out startOffset, out startLine, out startColumn, out fragmentLength);
            }
            else
            {
                // we do have length or precision, let's replace it
                PopulatePositionInfoFromAst(dataTypeAst.Parameters[0], out startOffset, out startLine, out startColumn, out fragmentLength);
            }

            AddScriptUpdateForElement(scriptUpdateList, column, newText, startOffset, startLine, startColumn, fragmentLength);

            return scriptUpdateList;
        }

        private static void GetUpdateInfoForLengthPrecisionScale(SqlDataTypeReference dataTypeAst, out int startOffset, out int startLine, out int startColumn, out int fragmentLength)
        {
            // a data type name can have more than one token:
            // CREATE TABLE t1 (c1 CHARACTER VARYING)
            TSqlParserToken lastToken = dataTypeAst.ScriptTokenStream[dataTypeAst.LastTokenIndex];
            int len = lastToken.Text.Length;
            startOffset = lastToken.Offset + len; // insert the length or precision[,scale] right after the data type name
            startLine = lastToken.Line;
            startColumn = lastToken.Column + len;
            fragmentLength = 0; // nothing to remove
        }

        private static void PopulatePositionInfoFromToken(TSqlParserToken token, out int offset, out int line, out int column, out int length)
        {
            offset = token.Offset;
            line = token.Line;
            column = token.Column;
            length = token.Text.Length;
        }

        private static void PopulatePositionInfoFromAst(TSqlFragment ast, out int offset, out int line, out int column, out int length)
        {
            offset = ast.StartOffset;
            line = ast.StartLine;
            column = ast.StartColumn;
            length = ast.FragmentLength;
        }

        private string GenerateScriptForDataType(SqlType dataType)
        {
            string text = string.Empty;

            SqlBuiltInType builtIn = dataType as SqlBuiltInType;
            if (builtIn != null)
            {
                // even though it looks more consistent with the else clause of this if statement 
                // if we go through script generator for builtin types, but it seems an overkill
                text = SqlModelUpdaterUtils.GetBuiltInTypeName(builtIn.SqlDataType).ToUpperInvariant();
            }
            else
            {
                // let script generator to generate the text for the type name
                ModelIdentifier typeName = dataType.Name;
                if (typeName == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Data type has no name");
                }
                else
                {
                    SchemaObjectName typeAst = ScriptDomUtils.CreateSchemaObjectName(typeName);
                    _scriptGenerator.GenerateScript(typeAst, out text);
                }
            }

            return text;
        }

        [Conditional("DEBUG")]
        private static void DBG_ValidateUpdateItems(IEnumerable<SqlScriptUpdateInfo> scriptUpdateList)
        {
            foreach (var item in scriptUpdateList)
            {
                int previousOffset = 0;
                int previousLength = 0;

                foreach (var update in item.Updates)
                {
                    if (previousOffset > update.StartOffset)
                    {
                        Debug.Assert(false, "Update list is not sorted by SqlScriptUpdateItem.StartOffset.");
                        break;
                    }

                    if (previousOffset + previousLength > update.StartOffset)
                    {
                        Debug.Assert(false, "Update list has overlapping");
                        break;
                    }

                    previousOffset = update.StartOffset;
                    previousLength = update.Length;
                }
            }
        }

        public IList<SqlScriptUpdateInfo> AddExtendedProperty(ISqlExtendedPropertyHost host, string name, string value)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            string script = GenerateStatementForExtendedProperty(host, name, value);

            scriptUpdateList.Add(SqlModelUpdaterUtils.AppendTextToFile(host, script));

            return scriptUpdateList;
        }

        private string GenerateStatementForExtendedProperty(ISqlExtendedPropertyHost host, string name, string value)
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();

            sfGen.AppendKeyword(TSqlTokenType.Exec);
            sfGen.AppendSpace();
            sfGen.AppendIdentifier(SqlModelUpdaterConstants.SP_ADDEXTENDEDPROPERTY, false);
            sfGen.AppendSpace();
            AppendNameValuePair(sfGen, SqlInterpretationConstants.AtName, name, newLineAndIndent: false, comma: true);
            AppendNameValuePair(sfGen, SqlInterpretationConstants.AtValue, value, newLineAndIndent: true, comma: true);

            int levelCount = SqlModelUpdaterConstants.ExtendedPropertyTypeParameterNames.Length; // 3: 3 levels
            for (int level = 0; level < levelCount; ++level)
            {
                // example: @level0type = 'SCHEMA',
                AppendNameValuePair(
                    sfGen,
                    SqlModelUpdaterConstants.ExtendedPropertyTypeParameterNames[level], // @level0type
                    SqlModelUpdaterConstants.ExtendedPropertyTypeParameterValues[host.ElementClass][level], // SCHEMA
                    newLineAndIndent: true,
                    comma: true);
                // example: @level0name = 'dbo', 
                AppendNameValuePair(
                    sfGen,
                    SqlModelUpdaterConstants.ExtendedPropertyNameParameterNames[level], // @level0Name
                    SqlModelUpdaterConstants.ExtendedPropertyNameParameterValues[host.ElementClass][level](host), // dbo
                    newLineAndIndent: true,
                    comma: level != levelCount - 1); // we don't add a comma for the last parameter
            }

            return sfGen.GetScriptFragment();
        }

        private void AppendNameValuePair(ScriptFragmentGenerator sfGen, string name, string value, bool newLineAndIndent, bool comma)
        {
            if (newLineAndIndent)
            {
                sfGen.AppendNewLine();
                sfGen.AppendSpace(SqlModelUpdaterConstants.Indent);
            }

            sfGen.AppendIdentifier(name, encode: false);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.EqualsSign);
            sfGen.AppendSpace();
            if (value == null)
            {
                sfGen.AppendKeyword(TSqlTokenType.Null);
            }
            else
            {
                sfGen.AppendText(GetStringForStringLiteral(value));
            }

            if (comma)
            {
                sfGen.AppendKeyword(TSqlTokenType.Comma);
            }
        }

        public IList<SqlScriptUpdateInfo> SetExtendedProperty(SqlExtendedProperty extendedProperty, string value)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();

            string valueString = GetStringForStringLiteral(value);

            ScalarExpression propertyValueAst = SqlModelUpdaterUtils.GetExtendedPropertyValueAst(extendedProperty);

            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(extendedProperty.PrimarySource.SourceName);
            scriptUpdate.AddUpdate(
                propertyValueAst.StartOffset,
                propertyValueAst.StartLine,
                propertyValueAst.StartColumn,
                propertyValueAst.FragmentLength,
                newText: valueString);

            scriptUpdateList.Add(scriptUpdate);

            return scriptUpdateList;
        }

        #region Manipulating Foreign Key Constraints

        #region Set Referenced Table
        public IList<SqlScriptUpdateInfo> SetForeignKeyForeignTable(SqlForeignKeyConstraint constraint, SqlTable referencedTable)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            ForeignKeyConstraintDefinition constraintAst = GetConstraintAst<ForeignKeyConstraintDefinition>(constraint);

            TSqlParserToken token = constraintAst.ScriptTokenStream[constraintAst.ReferenceTableName.FirstTokenIndex];

            SqlScriptUpdateInfo scriptUpdate = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
            scriptUpdate.AddUpdate(
                token.Offset,
                token.Line,
                token.Column,
                length: constraintAst.ReferenceTableName.FragmentLength, // replace text
                newText: GenerateSchemaQualifiedIdentifier(referencedTable));
            scriptUpdates.Add(scriptUpdate);
            return scriptUpdates;
        }
        #endregion

        #region Insert Referenced Column
        public IList<SqlScriptUpdateInfo> InsertForeignKeyForeignColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            ForeignKeyConstraintDefinition constraintAst = GetConstraintAst<ForeignKeyConstraintDefinition>(constraint);
            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find the constraint's AST");
            }

            // If the foreign column list does not exist, then add the list with the column added
            if (constraintAst.ReferencedTableColumns.Count == 0)
            {
                info.AddUpdate(ExpandReferencedColumnListForInsert(constraint, constraintAst, column, index));
            }
            else
            {
                // Otherwise, add it directly into the collection through the AST
                info.AddUpdate(InsertIntoDelimitedParenthesesWrappedList(
                    tokenStream: constraintAst.ScriptTokenStream,
                    nextTokenIndex: -1, // not used for this case, let's pass in an invalid value
                    collection: constraintAst.ReferencedTableColumns.ToArray(),
                    index: index,
                    elementBuilder: fragmentGenerator => fragmentGenerator.AppendIdentifier(column.Name.Parts.Last())));
            }

            return scriptUpdates;
        }
        #endregion

        #region Insert Referencing Column
        public IList<SqlScriptUpdateInfo> InsertForeignKeyColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            ForeignKeyConstraintDefinition constraintAst = GetConstraintAst<ForeignKeyConstraintDefinition>(constraint);
            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find the constraint's AST");
            }

            // First identify whether the local column list exists
            if (constraintAst.Columns.Count == 0)
            {
                // If it doesn't, then 'foreign key' may not exist. 
                // Since we know the column list doesn't exist, 'foreign key' must be immediately before 'references'.
                if (!DoesForeignKeyKeywordExist(constraintAst))
                {
                    info.AddUpdate(ExpandForeignKeyKeyword(constraint, constraintAst));
                }

                info.AddUpdate(ExpandReferencingColumnListForInsert(constraint, constraintAst, column, index));
            }
            else
            {
                // Otherwise, add it directly into the collection through the AST
                info.AddUpdate(InsertIntoDelimitedParenthesesWrappedList(
                    tokenStream: constraintAst.ScriptTokenStream,
                    nextTokenIndex: -1, // not used for this case, let's pass in an invalid value
                    collection: constraintAst.Columns.ToArray(),
                    index: index,
                    elementBuilder: fragmentGenerator => fragmentGenerator.AppendIdentifier(column.Name.Parts.Last())));
            }

            // We do not need to do anything with the referenced column list.

            return scriptUpdates;
        }
        #endregion

        #region Set Referenced Column
        public IList<SqlScriptUpdateInfo> SetForeignKeyForeignColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            ForeignKeyConstraintDefinition constraintAst = GetConstraintAst<ForeignKeyConstraintDefinition>(constraint);
            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find the constraint's AST");
            }

            // If the foreign column list does not exist, then add the list with the column added
            if (constraintAst.ReferencedTableColumns.Count == 0)
            {
                info.AddUpdate(ExpandReferencedColumnListForSet(constraint, constraintAst, newColumn, index));
            }
            else
            {
                // foreign columns are specified, let's just change it
                Identifier oldColumnAst = constraintAst.ReferencedTableColumns[index];

                ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
                sfGen.AppendIdentifier(newColumn.Name.Parts.Last());
                string newColumnText = sfGen.GetScriptFragment();

                info.AddUpdate(
                    oldColumnAst.StartOffset,
                    oldColumnAst.StartLine,
                    oldColumnAst.StartColumn,
                    oldColumnAst.FragmentLength, // delete old column ast
                    newColumnText);
            }

            return scriptUpdates;
        }
        #endregion

        #region Set Referencing Column
        public IList<SqlScriptUpdateInfo> SetForeignKeyColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index)
        {
            List<SqlScriptUpdateInfo> scriptUpdates = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
            scriptUpdates.Add(info);

            ForeignKeyConstraintDefinition constraintAst = GetConstraintAst<ForeignKeyConstraintDefinition>(constraint);
            if (constraintAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find the constraint's AST");
            }

            // First identify whether the local column list exists
            if (constraintAst.Columns.Count == 0)
            {
                // If it doesn't, then 'foreign key' may not exist. 
                // Since we know the column list doesn't exist, 'foreign key' must be immediately before 'references'.
                if (!DoesForeignKeyKeywordExist(constraintAst))
                {
                    info.AddUpdate(ExpandForeignKeyKeyword(constraint, constraintAst));
                }

                info.AddUpdate(ExpandReferencingColumnListForSet(constraint, constraintAst, newColumn, index));
            }
            else
            {
                // foreign columns are specified, let's just change it
                Identifier oldColumnAst = constraintAst.Columns[index];

                ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
                sfGen.AppendIdentifier(newColumn.Name.Parts.Last());
                string newColumnText = sfGen.GetScriptFragment();

                SqlScriptUpdateInfo updateInfo = new SqlScriptUpdateInfo(constraint.PrimarySource.SourceName);
                updateInfo.AddUpdate(
                    oldColumnAst.StartOffset,
                    oldColumnAst.StartLine,
                    oldColumnAst.StartColumn,
                    oldColumnAst.FragmentLength, // delete old column ast
                    newColumnText);
                scriptUpdates.Add(updateInfo);
            }

            // We do not need to do anything with the referenced column list.

            return scriptUpdates;
        }
        #endregion

        #region FK Helpers

        #region Probing Functions
        private static bool DoesForeignKeyKeywordExist(ForeignKeyConstraintDefinition constraintAst)
        {
            int tokenIndex;
            TSqlParserToken token;
            SqlModelUpdaterUtils.FindTokenBackward(constraintAst.ScriptTokenStream, constraintAst.ReferenceTableName.FirstTokenIndex, t => t.TokenType == TSqlTokenType.Foreign, out tokenIndex, out token);
            return token != null && tokenIndex >= constraintAst.FirstTokenIndex;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool IsConstraintColumnScoped(ForeignKeyConstraintDefinition constraintAst)
        {
            int tokenIndex;
            TSqlParserToken previousToken;

            SqlModelUpdaterUtils.FindTokenBackward(
                constraintAst.ScriptTokenStream,
                constraintAst.FirstTokenIndex - 1,
                t => SqlModelUpdaterUtils.IsSignificantToken(t),
                out tokenIndex,
                out previousToken);

            // If the previous token is not a comma or left parenthesis, this foreign key is column-scoped
            return (previousToken.TokenType != TSqlTokenType.Comma &&
                    previousToken.TokenType != TSqlTokenType.LeftParenthesis); // this is valid: create table t5 (foreign key (c1) references t1(c3), c1 int)
        }
        #endregion

        #region Expansion Helpers
        private static SqlScriptUpdateItem ExpandForeignKeyKeyword(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst)
        {
            // let's insert the foreign key keywords immediately before the references token
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendKeyword(TSqlTokenType.Foreign);
            sfGen.AppendSpace();
            sfGen.AppendKeyword(TSqlTokenType.Key);
            sfGen.AppendSpace();

            return InsertUpdateBeforeReferences(constraintAst, constraint.PrimarySource.SourceName, sfGen.GetScriptFragment());
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static SqlScriptUpdateItem ExpandReferencingColumn(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst)
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            GenerateColumnList(sfGen, constraint.Columns);
            string referencingColumnListText = sfGen.GetScriptFragment();

            // let's insert the local column list immediately before the references token
            return InsertUpdateBeforeReferences(constraintAst, constraint.PrimarySource.SourceName, String.Format(CultureInfo.CurrentCulture, "{0} ", referencingColumnListText));
        }

        private static SqlScriptUpdateItem ExpandReferencingColumnListForInsert(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst, SqlColumn column, int index)
        {
            // no columns are specified in the AST, we have to expand them
            string referencingColumnListText = GenerateColumnListWithInsert(constraint.Columns, column, index);

            // let's insert the local column list immediately before the references token
            return InsertUpdateBeforeReferences(constraintAst, constraint.PrimarySource.SourceName, String.Format(CultureInfo.CurrentCulture, "{0} ", referencingColumnListText));
        }

        private static SqlScriptUpdateItem ExpandReferencingColumnListForSet(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst, SqlColumn column, int index)
        {
            // no columns are specified in the AST, we have to expand them
            string referencingColumnListText = GenerateColumnListWithSet(constraint.Columns, column, index);

            // let's insert the local column list immediately before the references token
            return InsertUpdateBeforeReferences(constraintAst, constraint.PrimarySource.SourceName, String.Format(CultureInfo.CurrentCulture, "{0} ", referencingColumnListText));
        }

        private static SqlScriptUpdateItem InsertUpdateBeforeReferences(ForeignKeyConstraintDefinition constraintAst, string sourceName, string newText)
        {
            int tokenIndex;
            TSqlParserToken token;
            SqlModelUpdaterUtils.FindTokenBackward(constraintAst.ScriptTokenStream, constraintAst.ReferenceTableName.FirstTokenIndex, t => t.TokenType == TSqlTokenType.References, out tokenIndex, out token);

            SqlScriptUpdateItem item = new SqlScriptUpdateItem(
                token.Offset,
                token.Line,
                token.Column,
                0, // delete nothing
                newText);

            return item;
        }

        private static SqlScriptUpdateItem ExpandReferencedColumnListForInsert(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst, SqlColumn column, int index)
        {
            // no foreign columns are specified in the AST, we have to expand them
            string referencedColumnListText = GenerateColumnListWithInsert(constraint.ForeignColumns, column, index);

            // let's insert the foreign column list immediately after the foreign table
            return InsertUpdateAfterReferenceTable(constraintAst, constraint.PrimarySource.SourceName, referencedColumnListText);
        }

        private static SqlScriptUpdateItem ExpandReferencedColumnListForSet(SqlForeignKeyConstraint constraint, ForeignKeyConstraintDefinition constraintAst, SqlColumn column, int index)
        {
            // no foreign columns are specified in the AST, we have to expand them
            string referencedColumnListText = GenerateColumnListWithSet(constraint.ForeignColumns, column, index);

            // let's insert the foreign column list immediately after the foreign table
            return InsertUpdateAfterReferenceTable(constraintAst, constraint.PrimarySource.SourceName, referencedColumnListText);
        }

        private static SqlScriptUpdateItem InsertUpdateAfterReferenceTable(ForeignKeyConstraintDefinition constraintAst, string sourceName, string referencedColumnListText)
        {
            TSqlParserToken token = constraintAst.ScriptTokenStream[constraintAst.ReferenceTableName.LastTokenIndex + 1];
            SqlScriptUpdateItem item = new SqlScriptUpdateItem(
                token.Offset,
                token.Line,
                token.Column,
                0, // delete nothing
                referencedColumnListText);

            return item;
        }
        #endregion

        #region Generating Column Lists
        private static void GenerateColumnList(ScriptFragmentGenerator sfGen, IList<SqlColumn> columns, Action<ScriptFragmentGenerator> additionalAppendAction = null)
        {
            sfGen.AppendKeyword(TSqlTokenType.LeftParenthesis);

            int columnCount = columns.Count;
            for (int i = 0; i <= columnCount; i++)
            {
                if (i < columnCount)
                {
                    AppendIdentifierLastPart(sfGen, columns[i].Name, i == columnCount - 1, additionalAppendAction);
                }
            }
            sfGen.AppendKeyword(TSqlTokenType.RightParenthesis);
        }

        private static string GenerateColumnListWithInsert(IList<SqlColumn> columns, SqlColumn columnToBeInserted, int index)
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendKeyword(TSqlTokenType.LeftParenthesis);

            int columnCount = columns.Count;
            for (int i = 0; i <= columnCount; i++)
            {
                if (index == i)
                {
                    AppendIdentifierLastPart(sfGen, columnToBeInserted.Name, i == columnCount);
                }

                if (i < columnCount)
                {
                    AppendIdentifierLastPart(sfGen, columns[i].Name, i == columnCount - 1 && index != columnCount);
                }
            }
            sfGen.AppendKeyword(TSqlTokenType.RightParenthesis);
            return sfGen.GetScriptFragment();
        }

        private static string GenerateColumnListWithSet(IList<SqlColumn> columns, SqlColumn columnToBeSet, int index)
        {
            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendKeyword(TSqlTokenType.LeftParenthesis);

            int columnCount = columns.Count;
            for (int i = 0; i < columnCount; i++)
            {
                ModelIdentifier columnName = i == index ? columnToBeSet.Name : columns[i].Name;
                AppendIdentifierLastPart(sfGen, columnName, i == columnCount - 1);
            }
            sfGen.AppendKeyword(TSqlTokenType.RightParenthesis);
            return sfGen.GetScriptFragment();
        }

        private static void AppendIdentifierLastPart(ScriptFragmentGenerator sfGen, ModelIdentifier name, bool last, Action<ScriptFragmentGenerator> additionalAppendAction = null)
        {
            sfGen.AppendIdentifier(name.Parts.Last());
            if (additionalAppendAction != null)
            {
                additionalAppendAction(sfGen);
            }
            if (last == false)
            {
                sfGen.AppendKeyword(TSqlTokenType.Comma);
                sfGen.AppendSpace();
            }
        }
        #endregion

        #region General Helpers
        private static T GetConstraintAst<T>(SqlConstraint constraint) where T : ConstraintDefinition
        {
            TSqlFragment fragment = SqlModelUpdaterUtils.GetPrimaryAst<TSqlFragment>(constraint);
            ConstraintDefinition constraintAst = fragment as ConstraintDefinition;
            if (constraintAst == null)
            {
                AlterTableAddTableElementStatement alterTableAst = fragment as AlterTableAddTableElementStatement;
                if (alterTableAst != null)
                {
                    constraintAst = alterTableAst.Definition.TableConstraints[0] as ConstraintDefinition;
                }
            }

            return constraintAst as T;
        }
        #endregion

        #endregion

        #endregion

        private static SqlScriptUpdateItem DeleteModelElementPrimarySource(ISqlModelElement modelElement)
        {
            ISourceInformation primarySource = modelElement.PrimarySource;
            return new SqlScriptUpdateItem(
                primarySource.Offset,
                primarySource.StartLine,
                primarySource.StartColumn,
                primarySource.Length,
                newText: string.Empty);
        }

        public IList<SqlScriptUpdateInfo> DeleteExtendedProperty(SqlExtendedProperty extendedProperty)
        {
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(extendedProperty.PrimarySource.SourceName);
            info.AddUpdate(DeleteModelElementPrimarySource(extendedProperty));

            return new List<SqlScriptUpdateInfo> { info };
        }

        private static IList<SqlScriptUpdateItem> DeleteConstraintHelper(SqlConstraint constraint)
        {
            TSqlFragment fragment = SqlModelUpdaterUtils.GetPrimaryAst<TSqlFragment>(constraint);
            ConstraintDefinition constraintAst = fragment as ConstraintDefinition;
            if (constraintAst != null)
            {
                return SqlScriptUpdaterForTableElementDeletion.DeleteConstraint(constraint);
            }
            else
            {
                return new List<SqlScriptUpdateItem>() { DeleteModelElementPrimarySource(constraint) };
            }
        }

        public IList<SqlScriptUpdateInfo> DeleteConstraint(SqlConstraint defaultConstraint)
        {
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(defaultConstraint.PrimarySource.SourceName);
            info.AddUpdates(DeleteConstraintHelper(defaultConstraint));

            return new List<SqlScriptUpdateInfo> { info };
        }

        public IList<SqlScriptUpdateInfo> DeleteTable(SqlTable table)
        {
            return DeleteCreateAndAlterStatementsForElement(table);
        }

        public IList<SqlScriptUpdateInfo> DeleteView(SqlView view)
        {
            return DeleteCreateAndAlterStatementsForElement(view);
        }

        public IList<SqlScriptUpdateInfo> DeleteSubroutine(SqlSubroutine subroutine)
        {
            return DeleteCreateAndAlterStatementsForElement(subroutine);
        }

        private IList<SqlScriptUpdateInfo> DeleteCreateAndAlterStatementsForElement(ISqlModelElement element)
        {
            IList<SqlScriptUpdateInfo> scriptUpdateList = new List<SqlScriptUpdateInfo>();
            SqlScriptUpdateInfo info = new SqlScriptUpdateInfo(element.PrimarySource.SourceName);
            scriptUpdateList.Add(info);

            // First get the primary source's range
            string primarySourceName = element.PrimarySource.SourceName;

            info.AddUpdate(DeleteModelElementPrimarySource(element));

            SqlSchemaModel model = element.Model as SqlSchemaModel;
            TSqlParser parser = null;
            string fullScript = null;
            if (model != null)
            {
                parser = model.GetParser(element);
                fullScript = model.ScriptCache.GetScript(info.ScriptCacheIdentifier);

                SqlModelUpdaterUtils.RemoveWhitespaceAroundSourcePosition(info, parser, fullScript, element.PrimarySource);
            }

            // Now get the secondary sources' ranges
            foreach (ISourceInformation secondarySource in element.SecondarySources)
            {
                Debug.Assert(secondarySource.SourceName.Equals(primarySourceName, StringComparison.OrdinalIgnoreCase), "The supporting statement should be in the same file as the primary one");
                if (secondarySource.SourceName.Equals(primarySourceName, StringComparison.OrdinalIgnoreCase))
                {
                    info.AddUpdate(
                        secondarySource.Offset,
                        secondarySource.StartLine,
                        secondarySource.StartColumn,
                        secondarySource.Length,
                        newText: string.Empty);

                    if (parser != null && fullScript != null)
                    {
                        // Reuse fullScript from primary source as supporting statements should be in the same file as primary one.
                        SqlModelUpdaterUtils.RemoveWhitespaceAroundSourcePosition(info, parser, fullScript, secondarySource);
                    }
                }
            }

            return scriptUpdateList;
        }

        private string GetStringForStringLiteral(string value)
        {
            StringLiteral valueAst = ScriptDomUtils.CreateStringLiteral(value);
            string valueString;
            _scriptGenerator.GenerateScript(valueAst, out valueString);

            return valueString;
        }

        public static IList<SqlScriptUpdateInfo> RenameElement(ISqlModelElement element, string newName)
        {
            Dictionary<string, SqlScriptUpdateInfo> scriptUpdateMap = new Dictionary<string, SqlScriptUpdateInfo>();

            GetElementIdentifierAst getElementIndentifierAst;
            if (!_getElementIdentifierAction.TryGetValue(element.ElementClass, out getElementIndentifierAst))
            {
                SqlModelUpdaterUtils.TraceAndThrow("RenameElement() does not support element type " + element.ElementClass);
            }

            TSqlFragment elementAst = GetElementAst<TSqlFragment>(element);
            if (elementAst == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Could not find definition for element " + element.ElementClass);
            }

            VerifyTimestampSpecialCase(element, elementAst);

            Identifier identifierAst = getElementIndentifierAst(elementAst);
            AddUpdateForIdentifierRename(scriptUpdateMap, identifierAst, newName, element);

            SqlTable table = element as SqlTable;
            if (table != null)
            {
                RenameTableHierarchicalReferences(scriptUpdateMap, table, newName);
            }

            DBG_ValidateUpdateItems(scriptUpdateMap.Values);

            return scriptUpdateMap.Values.ToList();
        }

        private static void VerifyTimestampSpecialCase(ISqlModelElement element, TSqlFragment elementAst)
        {
            SqlSimpleColumn simpleColumn = element as SqlSimpleColumn;
            if (simpleColumn != null)
            {
                ColumnDefinition columnDef = (ColumnDefinition)elementAst;
                if (columnDef.DataType == null)
                {
                    // simple column definition with to type specification:
                    // this should be the case when a column is specified as "timestamp"
                    // and both the column name and type are determined to be "timestamp"

                    Debug.Assert(
                        simpleColumn.TypeSpecifier.Type is SqlBuiltInType &&
                        ((SqlBuiltInType)simpleColumn.TypeSpecifier.Type).SqlDataType == SqlDataType.Timestamp, "Column type should be timestamp.");

                    SqlModelUpdaterUtils.TraceAndThrow("Cannot rename column with implicit 'timestamp' identifier.");
                }
            }
        }

        private static void RenameTableHierarchicalReferences(Dictionary<string, SqlScriptUpdateInfo> scriptUpdateList, SqlTable table, string newName)
        {
            // for table, we also have to rename the references
            // to the table identifier for hierarchical relationships

            // constraints
            foreach (SqlConstraint constraint in table.Constraints)
            {
                AlterTableAddTableElementStatement alterTableAst = GetElementAst<AlterTableAddTableElementStatement>(constraint);
                if (alterTableAst != null)
                {
                    Identifier alterTableIdentifierAst = alterTableAst.SchemaObjectName.BaseIdentifier;
                    AddUpdateForIdentifierRename(scriptUpdateList, alterTableIdentifierAst, newName, constraint);
                }
            }

            // indexes 
            foreach (SqlIndex index in table.Indexes)
            {
                Identifier onTableAst = null;
                TSqlStatement indexStatement = GetElementAst<TSqlStatement>(index);
                IndexStatement createIndexAst = indexStatement as IndexStatement;
                if (createIndexAst != null)
                {
                    onTableAst = createIndexAst.OnName.BaseIdentifier;
                }
                else
                {
                    CreateSpatialIndexStatement createSpatialIndexAst = indexStatement as CreateSpatialIndexStatement;
                    if (createIndexAst != null)
                    {
                        onTableAst = createSpatialIndexAst.Object.BaseIdentifier;
                    }
                }

                if (onTableAst != null)
                {
                    AddUpdateForIdentifierRename(scriptUpdateList, onTableAst, newName, index);
                }
            }

            // fulltext indexes
            foreach (SqlFullTextIndex index in table.FullTextIndex)
            {
                CreateFullTextIndexStatement createFullTextIndexAst = GetElementAst<CreateFullTextIndexStatement>(index);
                if (createFullTextIndexAst != null)
                {
                    Identifier onTableAst = createFullTextIndexAst.OnName.BaseIdentifier;
                    AddUpdateForIdentifierRename(scriptUpdateList, onTableAst, newName, index);
                }
            }

            // DML triggers
            foreach (SqlDmlTrigger trigger in table.Triggers)
            {
                CreateTriggerStatement createTriggerAst = GetElementAst<CreateTriggerStatement>(trigger);
                if (createTriggerAst != null)
                {
                    Identifier onTableAst = createTriggerAst.TriggerObject.Name.BaseIdentifier;
                    AddUpdateForIdentifierRename(scriptUpdateList, onTableAst, newName, trigger);
                }
            }
        }

        private static void AddUpdateForIdentifierRename(Dictionary<string, SqlScriptUpdateInfo> scriptUpdateList, Identifier identifierAst, string newName, ISqlModelElement element)
        {
            if (identifierAst == null || identifierAst.FragmentLength == 0)
            {
                SqlModelUpdaterUtils.TraceAndThrow("cannot rename unnamed element");
            }

            ScriptFragmentGenerator sfGen = new ScriptFragmentGenerator();
            sfGen.AppendIdentifier(newName);

            string cacheId = element.PrimarySource.SourceName;

            SqlScriptUpdateInfo scriptUpdate;
            if (!scriptUpdateList.TryGetValue(cacheId, out scriptUpdate))
            {
                scriptUpdate = new SqlScriptUpdateInfo(cacheId);
                scriptUpdateList.Add(cacheId, scriptUpdate);
            }
            scriptUpdate.AddUpdate(identifierAst.StartOffset, identifierAst.StartLine, identifierAst.StartColumn, identifierAst.FragmentLength, sfGen.GetScriptFragment());
        }

    }
}