//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.BatchParser;

namespace Microsoft.SqlTools.ServiceLayer.Test.BatchParser
{
    internal sealed class TestVariableResolver : IVariableResolver
    {
        Dictionary<string, string> variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private StringBuilder _outputString;

        public TestVariableResolver(StringBuilder outputString)
        {
            _outputString = outputString;
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
            _outputString.AppendFormat("Setting variable {0} to [{1}]\n", name, value);
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
