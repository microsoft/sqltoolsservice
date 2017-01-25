//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.BatchParser;

namespace Microsoft.SqlTools.ServiceLayer.Test.BatchParser
{
    internal sealed class TestVariableResolver : IVariableResolver
    {
        Dictionary<string, string> variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private StringBuilder outputString;

        public TestVariableResolver(StringBuilder outputString)
        {
            outputString = outputString;
        }

        public string GetVariable(PositionStruct pos, string name)
        {
            if (variables.ContainsKey(name))
            {
                return variables[name];
            }
            else
            {
                return null;
            }
        }

        public void SetVariable(PositionStruct pos, string name, string value)
        {
            outputString.AppendFormat("Setting variable {0} to [{1}]\n", name, value);
            if (value == null)
            {
                variables.Remove(name);
            }
            else
            {
                variables[name] = value;
            }
        }
    }
}
