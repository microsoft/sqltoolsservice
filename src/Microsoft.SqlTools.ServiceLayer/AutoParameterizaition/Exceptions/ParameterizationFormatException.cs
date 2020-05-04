//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Helpers;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Exceptions
{
    /// <summary>
    /// ParameterizationFormatException is used to surface format exceptions encountered in the TSQL batch to perform 
    /// auto-parameterization of literals for Always Encrypted.
    /// </summary>
    public class ParameterizationFormatException : FormatException
    {
        public readonly int LineNumber;
        public readonly string LiteralValue;
        public readonly string CodeSenseMessage;
        public readonly string LogMessage;
        public readonly string SqlDatatype;
        public readonly string CSharpDataType;

        public ParameterizationFormatException(MessageHelper.MessageType type, string variableName, string sqlDataType, string cSharpDataType, string literalValue, int lineNumber)
            : this(type, variableName, sqlDataType, cSharpDataType, literalValue, lineNumber, exception: null) { }

        public ParameterizationFormatException(MessageHelper.MessageType type, string variableName, string sqlDataType, string cSharpDataType, string literalValue, int lineNumber, Exception exception)
            : base(MessageHelper.GetLocalizedMessage(type, variableName, sqlDataType, literalValue), exception)
        {
            LineNumber = lineNumber;
            LiteralValue = literalValue;
            SqlDatatype = sqlDataType;
            CSharpDataType = cSharpDataType;
            CodeSenseMessage = Message;
            LogMessage = MessageHelper.GetLocaleInvariantMessage(type, variableName, sqlDataType) + ", Literal Value: " + literalValue + ", On line: " + lineNumber + ", CSharp DataType: " + cSharpDataType;
        }
    }
}
