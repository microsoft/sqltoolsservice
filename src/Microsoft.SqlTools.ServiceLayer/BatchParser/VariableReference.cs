//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// Class for reference of variables used by the lexer
    /// </summary>
    internal sealed class VariableReference
    {
        /// <summary>
        /// Constructor method for VariableReference class
        /// </summary>
        public VariableReference(int start, int length, string variableName)
        {
            Start = start;
            Length = length;
            VariableName = variableName;
            VariableValue = null;
        }

        /// <summary>
        /// Get length associated with the VariableReference
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Get start position associated with the VariableReference
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Get variable name associated with the VariableReference
        /// </summary>
        public string VariableName { get; private set; }

        /// <summary>
        /// Get variable value associated with the VariableReference
        /// </summary>
        public string VariableValue { get; internal set; }
    }
}
