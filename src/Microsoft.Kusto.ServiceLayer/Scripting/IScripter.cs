//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    public interface IScripter
    {
        string SelectFromTableOrView(IDataSource dataSource, Urn urn);
        string AlterFunction(IDataSource dataSource, ScriptingObject scriptingObject);
        string ExecuteFunction(IDataSource dataSource, ScriptingObject scriptingObject);
    }
}