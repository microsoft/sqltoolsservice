//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal interface IVariableResolver
    {
        string GetVariable(PositionStruct pos, string name);
        void SetVariable(PositionStruct pos, string name, string value);
    }
}
