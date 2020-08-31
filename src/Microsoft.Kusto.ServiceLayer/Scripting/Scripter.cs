//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Text;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    [Export(typeof(IScripter))]
    public class Scripter : IScripter
    {
        public string SelectFromTableOrView(IDataSource dataSource, Urn urn)
        {
            StringBuilder selectQuery = new StringBuilder();

            // TODOKusto: Can we combine this with snippets. All queries generated here could also be snippets.
            // TODOKusto: Extract into the Kusto folder.
            selectQuery.Append($"{KustoQueryUtils.EscapeName(urn.GetAttribute("Name"))}");
            selectQuery.Append($"{KustoQueryUtils.StatementSeparator}");
            selectQuery.Append("limit 1000");

            return selectQuery.ToString();
        }

        public string AlterFunction(IDataSource dataSource, ScriptingObject scriptingObject)
        {
            var functionName = scriptingObject.Name.Substring(0, scriptingObject.Name.IndexOf('('));
            return dataSource.GenerateAlterFunctionScript(functionName);
        }
        
        public string ExecuteFunction(IDataSource dataSource, ScriptingObject scriptingObject)
        {
            var functionName = scriptingObject.Name.Substring(0, scriptingObject.Name.IndexOf('('));
            return dataSource.GenerateExecuteFunctionScript(functionName);
        }
    }
}