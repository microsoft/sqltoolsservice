//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public interface IVariableResolver
    {
        string GetVariable(PositionStruct pos, string name);
        void SetVariable(PositionStruct pos, string name, string value);
    }
}
