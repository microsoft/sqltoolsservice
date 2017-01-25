//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal sealed class VariableReference
    {
        public VariableReference(int start, int length, string variableName)
        {
            Start = start;
            Length = length;
            VariableName = variableName;
            VariableValue = null;
        }

        public int Length { get; private set; }

        public int Start { get; private set; }

        public string VariableName { get; private set; }

        public string VariableValue { get; internal set; }
    }
}
