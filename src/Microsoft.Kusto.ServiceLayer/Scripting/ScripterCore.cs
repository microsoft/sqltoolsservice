//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Text;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    internal partial class Scripter
    {
        private bool error;
        private string errorMessage;
        private IDataSource DataSource { get; set; }
        private ConnectionInfo connectionInfo;
        private string tempPath;

        // Dictionary that holds the object name (as appears on the TSQL create statement)
        private Dictionary<DeclarationType, string> sqlObjectTypes = new Dictionary<DeclarationType, string>();

        private Dictionary<string, string> sqlObjectTypesFromQuickInfo = new Dictionary<string, string>();

        private Dictionary<DatabaseEngineEdition, string> targetDatabaseEngineEditionMap = new Dictionary<DatabaseEngineEdition, string>();

        private Dictionary<int, string> serverVersionMap = new Dictionary<int, string>();

        private Dictionary<string, string> objectScriptMap = new Dictionary<string, string>();

        internal Scripter() {}

        /// <summary>
        /// Initialize a Peek Definition helper object
        /// </summary>
        /// <param name="dataSource">Data Source</param>
        internal Scripter(IDataSource dataSource, ConnectionInfo connInfo)
        {
            this.DataSource = dataSource;
            this.connectionInfo = connInfo;
            this.tempPath = FileUtilities.GetPeekDefinitionTempFolder();
            Initialize();
        }
        
        /// <summary>
        /// Add the given type, scriptgetter and the typeName string to the respective dictionaries
        /// </summary>
        private void AddSupportedType(DeclarationType type, string typeName, string quickInfoType, Type smoObjectType)
        {
            sqlObjectTypes.Add(type, typeName);
            if (!string.IsNullOrEmpty(quickInfoType))
            {
                sqlObjectTypesFromQuickInfo.Add(quickInfoType.ToLowerInvariant(), typeName);
            }
        }

        #region Helper Methods

        internal string SelectFromTableOrView(IDataSource dataSource, Urn urn)
        {
            StringBuilder selectQuery = new StringBuilder();

            // TODOKusto: Can we combine this with snippets. All queries generated here could also be snippets.
            // TODOKusto: Extract into the Kusto folder.
            selectQuery.Append($"{KustoQueryUtils.EscapeName(urn.GetAttribute("Name"))}");
            selectQuery.Append($"{KustoQueryUtils.StatementSeparator}");
            selectQuery.Append("limit 1000");

            return selectQuery.ToString();
        }
        
        internal string AlterFunction(IDataSource dataSource, ScriptingObject scriptingObject)
        {
            var functionName = scriptingObject.Name.Substring(0, scriptingObject.Name.IndexOf('('));
            return dataSource.GenerateAlterFunctionScript(functionName);
        }

        #endregion
    }
}