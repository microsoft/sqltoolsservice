//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition
{
    /// <summary>
    /// Entry point for SqlParameterization, this class is responsible for visiting the parse tree and identifying the scalar expressions to be parameterized
    /// </summary>
    internal class TsqlMultiVisitor : TSqlFragmentVisitor
    {
        private readonly ScalarExpressionTransformer ScalarExpressionTransformer;
        private readonly bool IsCodeSenseRequest;

        private Dictionary<string, int> _executionParameters = null;

        public List<SqlParameter> Parameters { get; private set; }

        public List<ScriptFileMarker> CodeSenseMessages { get; private set; }

        public List<ScriptFileMarker> CodeSenseErrors { get; private set; }

        public Dictionary<string, int> ExecutionParameters
        {
            get
            {
                if (_executionParameters == null)
                {
                    _executionParameters = new Dictionary<string, int>();
                }

                return _executionParameters;
            }
        }

        public TsqlMultiVisitor(bool isCodeSenseRequest)
        {
            Parameters = new List<SqlParameter>();
            IsCodeSenseRequest = isCodeSenseRequest;
            CodeSenseMessages = new List<ScriptFileMarker>();
            CodeSenseErrors = new List<ScriptFileMarker>();
            ScalarExpressionTransformer = new ScalarExpressionTransformer(isCodeSenseRequest, CodeSenseErrors);
        }

        public override void ExplicitVisit(DeclareVariableStatement node)
        {
            if (node == null || node.Declarations == null)
            {
                return;
            }

            StringBuilder codeSenseMessageStringBuilder = new StringBuilder();
            int endLine = -1;
            int endCol = -1;

            foreach (DeclareVariableElement declareVariableElement in node.Declarations)
            {
                if (declareVariableElement.DataType is SqlDataTypeReference dataTypeReference)
                {
                    SqlDataTypeOption sqlDataTypeOption = dataTypeReference.SqlDataTypeOption;

                    if (ShouldParamterize(sqlDataTypeOption))
                    {
                        IList<Literal> sqlDataTypeParameters = dataTypeReference.Parameters;
                        ScalarExpressionTransformer.SqlDataTypeOption = sqlDataTypeOption;
                        ScalarExpressionTransformer.SqlDataTypeParameters = sqlDataTypeParameters;
                        ScalarExpressionTransformer.VariableName = declareVariableElement.VariableName.Value;

                        ScalarExpression declareVariableElementValue = declareVariableElement.Value;
                        ScalarExpressionTransformer.ExplicitVisit(declareVariableElementValue);
                        declareVariableElement.Value = ScalarExpressionTransformer.GetTransformedExpression();
                        IList<SqlParameter> sqlParameters = ScalarExpressionTransformer.Parameters;

                        if (sqlParameters.Count == 1 && declareVariableElementValue != null)
                        {
                            codeSenseMessageStringBuilder.Append(SR.ParameterizationDetails(declareVariableElement.VariableName.Value,
                                                                                            sqlParameters[0].SqlDbType.ToString(),
                                                                                            sqlParameters[0].Size,
                                                                                            sqlParameters[0].Precision,
                                                                                            sqlParameters[0].Scale,
                                                                                            sqlParameters[0].SqlValue.ToString()));

                            endLine = declareVariableElementValue.StartLine;
                            endCol = declareVariableElementValue.StartColumn + declareVariableElementValue.FragmentLength;

                            if (!IsCodeSenseRequest)
                            {
                                string sqlParameterKey = sqlParameters[0].SqlDbType.ToString();
                                ExecutionParameters.TryGetValue(sqlParameterKey, out int currentCount);
                                ExecutionParameters[sqlParameterKey] = currentCount + 1;
                            }
                        }

                        Parameters.AddRange(sqlParameters);
                        ScalarExpressionTransformer.Reset();
                    }
                }
            }

            if (codeSenseMessageStringBuilder.Length > 0)
            {
                CodeSenseMessages.Add(new ScriptFileMarker
                {
                    Level = ScriptFileMarkerLevel.Information,
                    Message = codeSenseMessageStringBuilder.ToString(),
                    ScriptRegion = new ScriptRegion
                    {
                        StartLineNumber = node.StartLine,
                        StartColumnNumber = node.StartColumn,
                        EndLineNumber = endLine == -1 ? node.StartLine : endLine,
                        EndColumnNumber = endCol == -1 ? node.StartColumn + node.LastTokenIndex - node.FirstTokenIndex : endCol
                    }
                });
            }

            node.AcceptChildren(this);
            base.ExplicitVisit(node); // let the base class finish up
        }

        private bool ShouldParamterize(SqlDataTypeOption sqlDataTypeOption)
        {
            switch (sqlDataTypeOption)
            {
                case SqlDataTypeOption.BigInt:
                case SqlDataTypeOption.Int:
                case SqlDataTypeOption.SmallInt:
                case SqlDataTypeOption.TinyInt:
                case SqlDataTypeOption.Bit:
                case SqlDataTypeOption.Decimal:
                case SqlDataTypeOption.Numeric:
                case SqlDataTypeOption.Money:
                case SqlDataTypeOption.SmallMoney:
                case SqlDataTypeOption.Float:
                case SqlDataTypeOption.Real:
                case SqlDataTypeOption.DateTime:
                case SqlDataTypeOption.SmallDateTime:
                case SqlDataTypeOption.Char:
                case SqlDataTypeOption.VarChar:
                case SqlDataTypeOption.NChar:
                case SqlDataTypeOption.NVarChar:
                case SqlDataTypeOption.Binary:
                case SqlDataTypeOption.VarBinary:
                case SqlDataTypeOption.UniqueIdentifier:
                case SqlDataTypeOption.Date:
                case SqlDataTypeOption.Time:
                case SqlDataTypeOption.DateTime2:
                case SqlDataTypeOption.DateTimeOffset:
                    return true;

                default:
                    return false;
            }
        }

        public void Reset()
        {
            Parameters.Clear();
            CodeSenseMessages.Clear();
            CodeSenseErrors.Clear();
            ExecutionParameters.Clear();
        }
    }
}
