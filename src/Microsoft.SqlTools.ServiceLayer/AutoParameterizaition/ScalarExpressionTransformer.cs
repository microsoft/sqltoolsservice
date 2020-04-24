//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Exceptions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Helpers;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition
{
    internal class ScalarExpressionTransformer : TSqlFragmentVisitor
    {
        #region datetimeFormats

        private static readonly string[] SUPPORTED_ISO_DATE_TIME_FORMATS = {
            "yyyyMMdd HH:mm:ss.fffffff",
            "yyyyMMdd HH:mm:ss.ffffff",
            "yyyyMMdd HH:mm:ss.fffff",
            "yyyyMMdd HH:mm:ss.ffff",
            "yyyyMMdd HH:mm:ss.fff",
            "yyyyMMdd HH:mm:ss.ff",
            "yyyyMMdd HH:mm:ss.f",
            "yyyyMMdd HH:mm:ss",
            "yyyyMMdd HH:mm",
            "yyyyMMdd",

            "yyyy-MM-ddTHH:mm:ss.fffffff",
            "yyyy-MM-ddTHH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.fffff",
            "yyyy-MM-ddTHH:mm:ss.ffff",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-dd",
        };

        private static readonly string[] SUPPORTED_ISO_DATE_FORMATS = {
            "yyyyMMdd",
            "yyyy-MM-dd",
        };

        private static readonly string[] SUPPORTED_ISO_DATE_TIME_OFFSET_FORMATS = { 
            // 121025 12:32:10.1234567 +01:00 – zzz in the below format represents +01:00
            "yyyyMMdd HH:mm:ss.fffffff zzz",
            "yyyyMMdd HH:mm:ss.ffffff zzz",
            "yyyyMMdd HH:mm:ss.fffff zzz",
            "yyyyMMdd HH:mm:ss.ffff zzz",
            "yyyyMMdd HH:mm:ss.fff zzz",
            "yyyyMMdd HH:mm:ss.ff zzz",
            "yyyyMMdd HH:mm:ss.f zzz",
            "yyyyMMdd HH:mm:ss zzz",
            "yyyyMMdd HH:mm zzz",
            "yyyyMMdd zzz",

            "yyyy-MM-ddTHH:mm:ss.fffffff zzz",
            "yyyy-MM-ddTHH:mm:ss.ffffff zzz",
            "yyyy-MM-ddTHH:mm:ss.fffff zzz",
            "yyyy-MM-ddTHH:mm:ss.ffff zzz",
            "yyyy-MM-ddTHH:mm:ss.fff zzz",
            "yyyy-MM-ddTHH:mm:ss.ff zzz",
            "yyyy-MM-ddTHH:mm:ss.f zzz",
            "yyyy-MM-ddTHH:mm:ss zzz",
            "yyyy-MM-ddTHH:mm zzz",
            "yyyy-MM-dd zzz",

            //19991212 19:30:30.1234567Z – K  in the below format represents Z
            "yyyyMMdd HH:mm:ss.fffffffK",
            "yyyyMMdd HH:mm:ss.ffffffK",
            "yyyyMMdd HH:mm:ss.fffffK",
            "yyyyMMdd HH:mm:ss.ffffK",
            "yyyyMMdd HH:mm:ss.fffK",
            "yyyyMMdd HH:mm:ss.ffK",
            "yyyyMMdd HH:mm:ss.fK",
            "yyyyMMdd HH:mm:ssK",
            "yyyyMMdd HH:mmK",
            "yyyyMMddK",

            "yyyy-MM-ddTHH:mm:ss.fffffffK",
            "yyyy-MM-ddTHH:mm:ss.ffffffK",
            "yyyy-MM-ddTHH:mm:ss.fffffK",
            "yyyy-MM-ddTHH:mm:ss.ffffK",
            "yyyy-MM-ddTHH:mm:ss.fffK",
            "yyyy-MM-ddTHH:mm:ss.ffK",
            "yyyy-MM-ddTHH:mm:ss.fK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddK",
        };

        #endregion

        private const string C_SHARP_BYTE_ARRAY = "byte[]";
        private readonly bool IsCodeSenseRequest;
        private bool IsNegative = false;
        private ScalarExpression CurrentScalarExpression;
        public SqlDataTypeOption SqlDataTypeOption;
        public IList<Literal> SqlDataTypeParameters;
        public string VariableName;

        public IList<SqlParameter> Parameters { get; private set; }

        private readonly IList<ScriptFileMarker> CodeSenseErrors;

        public ScalarExpressionTransformer(bool isCodeSenseRequest, IList<ScriptFileMarker> codeSenseErrors)
        {
            Parameters = new List<SqlParameter>();
            IsCodeSenseRequest = isCodeSenseRequest;
            CodeSenseErrors = codeSenseErrors;
        }

        public override void ExplicitVisit(ScalarExpression node)
        {
            if (node == null)
            {
                return;
            }

            CurrentScalarExpression = node;

            if (node is Literal literal)
            {

                if (ShouldParameterize(literal))
                {
                    var variableReference = new VariableReference();
                    string parameterName = GetParameterName();
                    variableReference.Name = parameterName;
                    AddToParameterCollection(literal, parameterName, SqlDataTypeOption, SqlDataTypeParameters);
                    CurrentScalarExpression = variableReference;
                }

                return;
            }


            if (node is UnaryExpression unaryExpression)
            {
                ScalarExpression expression = unaryExpression.Expression;

                if (expression != null)
                {
                    if (unaryExpression.UnaryExpressionType.Equals(UnaryExpressionType.Negative))
                    {
                        IsNegative = !IsNegative;
                    }

                    ExplicitVisit(expression);
                }

                base.ExplicitVisit(node); //let the base class finish up
                return;
            }


            if (node is ParenthesisExpression parenthesisExpression)
            {

                ScalarExpression expression = parenthesisExpression.Expression;

                if (expression != null)
                {
                    ScalarExpression tempScalarExpression = CurrentScalarExpression;
                    ExplicitVisit(expression);
                    parenthesisExpression.Expression = GetTransformedExpression();
                    CurrentScalarExpression = tempScalarExpression;
                }

                base.ExplicitVisit(node); // let the base class finish up
            }
        }

        public ScalarExpression GetTransformedExpression()
        {
            return CurrentScalarExpression;
        }

        public void Reset()
        {
            SqlDataTypeOption = SqlDataTypeOption.VarChar;
            SqlDataTypeParameters = null;
            VariableName = null;
            IsNegative = false;
            Parameters.Clear();
        }

        /// <summary>
        /// Converts a hex string to a byte[]
        /// Note: this method expects "0x" prefix to be stripped off from the input string
        /// For example, to convert the string "0xFFFF" to byte[] the input to this method should be "FFFF"
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private void AddToParameterCollection(Literal literal, string parameterName, SqlDataTypeOption sqlDataTypeOption, IList<Literal> sqlDataTypeParameters)
        {
            SqlParameter sqlParameter = new SqlParameter();
            string literalValue = literal.Value;
            object parsedValue = null;
            SqlDbType paramType = SqlDbType.VarChar;
            bool parseSuccessful = true;

            switch (sqlDataTypeOption)
            {
                case SqlDataTypeOption.Binary:
                    paramType = SqlDbType.Binary;
                    try
                    {
                        parsedValue = TryParseBinaryLiteral(literalValue, VariableName, SqlDbType.Binary, literal.StartLine);
                    }
                    catch (ParameterizationFormatException)
                    {
                        if (IsCodeSenseRequest)
                        {
                            parseSuccessful = false;
                            AddCodeSenseErrorItem(MessageHelper.MessageType.BINARY_LITERAL_PREFIX_MISSING_ERROR, literal, literal.Value, VariableName, SqlDbType.Binary.ToString());
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Binary.ToString(), C_SHARP_BYTE_ARRAY, literalValue, literal.StartLine, e);
                    }
                    break;

                case SqlDataTypeOption.VarBinary:
                    paramType = SqlDbType.VarBinary;
                    try
                    {
                        parsedValue = TryParseBinaryLiteral(literalValue, VariableName, SqlDbType.VarBinary, literal.StartLine);
                        ExtractSize(sqlDataTypeParameters, sqlParameter);
                    }
                    catch (ParameterizationFormatException)
                    {
                        if (IsCodeSenseRequest)
                        {
                            parseSuccessful = false;
                            string sqlDataTypeString = GetSqlDataTypeStringOneParameter(SqlDbType.VarBinary, sqlDataTypeParameters);
                            AddCodeSenseErrorItem(MessageHelper.MessageType.BINARY_LITERAL_PREFIX_MISSING_ERROR, literal, literalValue, VariableName, sqlDataTypeString);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        string sqlDataTypeString = GetSqlDataTypeStringOneParameter(SqlDbType.VarBinary, sqlDataTypeParameters);
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, sqlDataTypeString, C_SHARP_BYTE_ARRAY, literalValue, literal.StartLine, e);
                    }
                    break;


                //Integer literals of form 24.0 will not be supported
                case SqlDataTypeOption.BigInt:
                    paramType = SqlDbType.BigInt;
                    long parsedLong;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    if (long.TryParse(literalValue, out parsedLong))
                    {
                        parsedValue = parsedLong;
                    }
                    else
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.BigInt.ToString(), "Int64", literalValue, literal.StartLine, null);
                    }

                    break;

                case SqlDataTypeOption.Int:
                    paramType = SqlDbType.Int;
                    int parsedInt;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    if (int.TryParse(literalValue, out parsedInt))
                    {
                        parsedValue = parsedInt;
                    }
                    else
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Int.ToString(), "Int32", literalValue, literal.StartLine, null);
                    }

                    break;

                case SqlDataTypeOption.SmallInt:
                    paramType = SqlDbType.SmallInt;
                    short parsedShort;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    if (short.TryParse(literalValue, out parsedShort))
                    {
                        parsedValue = parsedShort;
                    }
                    else
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.SmallInt.ToString(), "Int16", literalValue, literal.StartLine, null);
                    }

                    break;

                case SqlDataTypeOption.TinyInt:
                    paramType = SqlDbType.TinyInt;
                    byte parsedByte;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    if (byte.TryParse(literalValue, out parsedByte))
                    {
                        parsedValue = parsedByte;
                    }
                    else
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.TinyInt.ToString(), "Byte", literalValue, literal.StartLine, null);
                    }

                    break;


                case SqlDataTypeOption.Real:
                    paramType = SqlDbType.Real;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    try
                    {
                        parsedValue = SqlSingle.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Real.ToString(), "SqlSingle", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.Float:
                    paramType = SqlDbType.Float;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    try
                    {
                        parsedValue = SqlDouble.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Float.ToString(), "SqlDouble", literalValue, literal.StartLine, e);
                    }

                    break;


                case SqlDataTypeOption.Decimal:
                case SqlDataTypeOption.Numeric:
                    paramType = SqlDbType.Decimal;
                    ExtractPrecisionAndScale(sqlDataTypeParameters, sqlParameter);
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    try
                    {
                        parsedValue = SqlDecimal.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        string sqlDecimalDataType = sqlDataTypeParameters != null ? (SqlDbType.Decimal + "(" + sqlDataTypeParameters[0] + ", " + sqlDataTypeParameters[1] + ")") : "";
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, sqlDecimalDataType, "SqlDecimal", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.Money:
                    paramType = SqlDbType.Money;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    try
                    {
                        parsedValue = SqlMoney.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Money.ToString(), "SqlMoney", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.SmallMoney:
                    paramType = SqlDbType.SmallMoney;
                    literalValue = IsNegative ? "-" + literalValue : literalValue;

                    try
                    {
                        parsedValue = SqlMoney.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.SmallMoney.ToString(), "SqlMoney", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.DateTime:
                    paramType = SqlDbType.DateTime;

                    try
                    {
                        parsedValue = ParseDateTime(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.DATE_TIME_ERROR_MESSAGE, literal, VariableName, SqlDbType.DateTime.ToString(), "DateTime", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.SmallDateTime:
                    paramType = SqlDbType.SmallDateTime;

                    try
                    {
                        parsedValue = ParseDateTime(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.DATE_TIME_ERROR_MESSAGE, literal, VariableName, SqlDbType.SmallDateTime.ToString(), "DateTime", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.DateTime2:
                    paramType = SqlDbType.DateTime2;

                    try
                    {
                        parsedValue = ParseDateTime(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.DATE_TIME_ERROR_MESSAGE, literal, VariableName, SqlDbType.DateTime2.ToString(), "DateTime", literalValue, literal.StartLine, e);
                    }

                    ExtractPrecision(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.Date:
                    paramType = SqlDbType.Date;

                    try
                    {
                        parsedValue = ParseDate(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.DATE_TIME_ERROR_MESSAGE, literal, VariableName, SqlDbType.Date.ToString(), "DateTime", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.DateTimeOffset:
                    paramType = SqlDbType.DateTimeOffset;

                    try
                    {
                        parsedValue = ParseDateTimeOffset(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.DATE_TIME_ERROR_MESSAGE, literal, VariableName, SqlDbType.DateTimeOffset.ToString(), "DateTimeOffset", literalValue, literal.StartLine, e);
                    }

                    ExtractPrecision(sqlDataTypeParameters, sqlParameter);
                    break;


                case SqlDataTypeOption.Time:
                    paramType = SqlDbType.Time;

                    try
                    {
                        parsedValue = TimeSpan.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Time.ToString(), "TimeSpan", literalValue, literal.StartLine, e);
                    }

                    ExtractPrecision(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.Char:
                    paramType = SqlDbType.Char;
                    ExtractSize(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.VarChar:
                    paramType = SqlDbType.VarChar;
                    ExtractSize(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.NChar:
                    paramType = SqlDbType.NChar;
                    ExtractSize(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.NVarChar:
                    paramType = SqlDbType.NVarChar;
                    ExtractSize(sqlDataTypeParameters, sqlParameter);
                    break;

                case SqlDataTypeOption.UniqueIdentifier:
                    paramType = SqlDbType.UniqueIdentifier;

                    try
                    {
                        parsedValue = SqlGuid.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.UniqueIdentifier.ToString(), "SqlGuid", literalValue, literal.StartLine, e);
                    }

                    break;

                case SqlDataTypeOption.Bit:
                    paramType = SqlDbType.Bit;

                    try
                    {
                        parsedValue = Byte.Parse(literalValue);
                    }
                    catch (Exception e)
                    {
                        parseSuccessful = false;
                        HandleError(MessageHelper.MessageType.ERROR_MESSAGE, literal, VariableName, SqlDbType.Bit.ToString(), "Byte", literalValue, literal.StartLine, e);
                    }
                    break;

                default:
                    break;
            }

            if (parseSuccessful)
            {
                sqlParameter.ParameterName = parameterName;
                sqlParameter.SqlDbType = paramType;
                sqlParameter.Value = parsedValue ?? literalValue;
                sqlParameter.Direction = ParameterDirection.Input;
                Parameters.Add(sqlParameter);
            }
        }

        private string GetSqlDataTypeStringOneParameter(SqlDbType sqlDataType, IList<Literal> sqlDataTypeParameters)
        {
            string parameters = sqlDataTypeParameters != null ? ("(" + sqlDataTypeParameters[0] + ")") : "";
            return sqlDataType + parameters;
        }

        private void HandleError(MessageHelper.MessageType errorMessage, Literal literal, string variableName, string sqlDbType, string cSharpType, string literalValue, int startLine, Exception exception)
        {
            if (IsCodeSenseRequest)
            {
                AddCodeSenseErrorItem(errorMessage, literal, literalValue, variableName, sqlDbType);
            }
            else
            {
                if (exception != null)
                {
                    throw new ParameterizationFormatException(errorMessage, variableName, sqlDbType, cSharpType, literalValue, startLine, exception);
                }
                else
                {
                    throw new ParameterizationFormatException(errorMessage, variableName, sqlDbType, cSharpType, literalValue, startLine);
                }
            }
        }

        private void AddCodeSenseErrorItem(MessageHelper.MessageType messageType, Literal literal, string literalValue, string variableName, string sqlDbType)
        {
            CodeSenseErrors.Add(new ScriptFileMarker
            {
                Level = ScriptFileMarkerLevel.Error,
                Message = MessageHelper.GetLocalizedMessage(messageType, variableName, sqlDbType, literalValue),
                ScriptRegion = new ScriptRegion
                {
                    StartLineNumber = literal.StartLine,
                    StartColumnNumber = literal.StartColumn,
                    EndLineNumber = literal.StartLine,
                    EndColumnNumber = literal.StartColumn + literalValue.Length
                }
            });
        }

        private object ParseDateTime(string literalValue)
        {
            return DateTime.ParseExact(literalValue, SUPPORTED_ISO_DATE_TIME_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        private object ParseDate(string literalValue)
        {
            return DateTime.ParseExact(literalValue, SUPPORTED_ISO_DATE_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        private object ParseDateTimeOffset(string literalValue)
        {
            return DateTimeOffset.ParseExact(literalValue, SUPPORTED_ISO_DATE_TIME_OFFSET_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        private bool ShouldParameterize(Literal literal)
        {
            switch (literal.LiteralType)
            {
                case LiteralType.Integer:
                case LiteralType.Real:
                case LiteralType.Money:
                case LiteralType.Binary:
                case LiteralType.String:
                case LiteralType.Numeric:
                    return true;

                default:
                    return false;
            }
        }

        private void ExtractPrecisionAndScale(IList<Literal> dataTypeParameters, SqlParameter sqlParameter)
        {
            if (dataTypeParameters != null && dataTypeParameters.Count == 2)
            {
                Literal precisionLiteral = dataTypeParameters[0];

                if (byte.TryParse(precisionLiteral.Value, out byte precision))
                {
                    sqlParameter.Precision = precision;
                }

                Literal scaleLiteral = dataTypeParameters[1];

                if (byte.TryParse(scaleLiteral.Value, out byte scale))
                {
                    sqlParameter.Scale = scale;
                }
            }
        }

        private void ExtractPrecision(IList<Literal> dataTypeParameters, SqlParameter sqlParameter)
        {
            if (dataTypeParameters != null && dataTypeParameters.Count == 1)
            {
                Literal precisionLiteral = dataTypeParameters[0];

                if (byte.TryParse(precisionLiteral.Value, out byte precision))
                {
                    sqlParameter.Precision = precision;
                }
            }
        }

        private void ExtractSize(IList<Literal> dataTypeParameters, SqlParameter sqlParameter)
        {
            if (dataTypeParameters != null && dataTypeParameters.Count == 1)
            {
                Literal sizeLiteral = dataTypeParameters[0];

                if (int.TryParse(sizeLiteral.Value, out int size))
                {
                    sqlParameter.Size = size;
                }
            }
        }

        private object TryParseBinaryLiteral(string literalValue, string variableName, SqlDbType sqlDbType, int lineNumber)
        {
            if (literalValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                string hexString = literalValue.Substring(2);
                return StringToByteArray(hexString);
            }

            throw new ParameterizationFormatException(MessageHelper.MessageType.BINARY_LITERAL_PREFIX_MISSING_ERROR, variableName, sqlDbType.ToString(), C_SHARP_BYTE_ARRAY, literalValue, lineNumber);
        }

        private string GetParameterName()
        {
            return "@p" + Guid.NewGuid().ToString("N"); //option N will give a guid without dashes
        }
    }
}
