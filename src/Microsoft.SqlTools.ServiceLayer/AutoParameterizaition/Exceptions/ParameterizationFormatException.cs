//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Helpers;
using System;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Exceptions
{
    /// <summary>
    /// ParameterizationFormatException is used to surface format exceptions encountered in the TSQL batch to perform auto-parameterization of literals
    /// </summary>
    class ParameterizationFormatException : FormatException
    {
        public readonly int LineNumber;
        public readonly string LiteralValue;
        public readonly string CodeSenseMessage;
        public readonly string LogMessage;
        public readonly string TelemetryMessage;
        public readonly string SqlDatatype;
        public readonly string CSharpDataType;

        public ParameterizationFormatException(MessageHelper.MessageType type, string variableName, string sqlDataType, string cSharpDataType, string literalValue, int lineNumber)
            : this(type, variableName, sqlDataType, cSharpDataType, literalValue, lineNumber, null)
        {
        }

        public ParameterizationFormatException(MessageHelper.MessageType type, string variableName, string sqlDataType, string cSharpDataType, string literalValue, int lineNumber, Exception e)
            : base(MessageHelper.GetLocalizedMessage(type, variableName, sqlDataType, literalValue), e)
        {
            LineNumber = lineNumber;
            LiteralValue = literalValue;
            SqlDatatype = sqlDataType;
            CSharpDataType = cSharpDataType;
            CodeSenseMessage = this.Message;
            LogMessage = MessageHelper.GetLocaleInvariantMessage(type, variableName, sqlDataType) + ", Literal Value: " + literalValue + ", On line: " + lineNumber + ", CSharp DataType: " + cSharpDataType;
            TelemetryMessage = MessageHelper.GetLocaleInvariantMessage(type, variableName, sqlDataType);
        }
    }
}
