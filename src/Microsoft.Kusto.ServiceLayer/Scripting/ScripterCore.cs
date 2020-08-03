//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Location = Microsoft.Kusto.ServiceLayer.Workspace.Contracts.Location;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Text;
using System.Data;
using Range = Microsoft.Kusto.ServiceLayer.Workspace.Contracts.Range;

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

        /// <summary>
        /// Get the script of the selected token based on the type of the token
        /// </summary>
        /// <param name="declarationItems"></param>
        /// <param name="tokenText"></param>
        /// <param name="schemaName"></param>
        /// <returns>Location object of the script file</returns>
        internal DefinitionResult GetScript(ParseResult parseResult, Position position, IMetadataDisplayInfoProvider metadataDisplayInfoProvider, string tokenText, string schemaName)
        {
            int parserLine = position.Line;
            int parserColumn = position.Character;
            // Get DeclarationItems from The Intellisense Resolver for the selected token. The type of the selected token is extracted from the declarationItem.
            IEnumerable<Declaration> declarationItems = GetCompletionsForToken(parseResult, parserLine, parserColumn, metadataDisplayInfoProvider);
            if (declarationItems != null && declarationItems.Count() > 0)
            {
                foreach (Declaration declarationItem in declarationItems)
                {
                    if (declarationItem.Title == null)
                    {
                        continue;
                    }
                    
                    StringComparison caseSensitivity = StringComparison.OrdinalIgnoreCase;
                    // if declarationItem matches the selected token, script SMO using that type

                    if (declarationItem.Title.Equals(tokenText, caseSensitivity))
                    {
                        return GetDefinitionUsingDeclarationType(declarationItem.Type, declarationItem.DatabaseQualifiedName, tokenText, schemaName);
                    }
                }
            }
            else
            {
                // if no declarationItem matched the selected token, we try to find the type of the token using QuickInfo.Text
                string quickInfoText = GetQuickInfoForToken(parseResult, parserLine, parserColumn, metadataDisplayInfoProvider);
                return GetDefinitionUsingQuickInfoText(quickInfoText, tokenText, schemaName);
            }
            // no definition found
            return GetDefinitionErrorResult(SR.PeekDefinitionNoResultsError);
        }

        /// <summary>
        /// Script an object using the type extracted from quickInfo Text
        /// </summary>
        /// <param name="quickInfoText">the text from the quickInfo for the selected token</param>
        /// <param name="tokenText">The text of the selected token</param>
        /// <param name="schemaName">Schema name</param>
        /// <returns></returns>
        internal DefinitionResult GetDefinitionUsingQuickInfoText(string quickInfoText, string tokenText, string schemaName)
        {
            StringComparison caseSensitivity = StringComparison.OrdinalIgnoreCase;
            string tokenType = GetTokenTypeFromQuickInfo(quickInfoText, tokenText, caseSensitivity);
            if (tokenType != null)
            {
                if (sqlObjectTypesFromQuickInfo.ContainsKey(tokenType.ToLowerInvariant()))
                {
                    // With SqlLogin authentication, the defaultSchema property throws an Exception when accessed.
                    // This workaround ensures that a schema name is present by attempting
                    // to get the schema name from the declaration item.
                    // If all fails, the default schema name is assumed to be "dbo"
                    if ((connectionInfo != null && connectionInfo.ConnectionDetails.AuthenticationType.Equals(Constants.SqlLoginAuthenticationType)) && string.IsNullOrEmpty(schemaName))
                    {
                        string fullObjectName = this.GetFullObjectNameFromQuickInfo(quickInfoText, tokenText, caseSensitivity);
                        schemaName = this.GetSchemaFromDatabaseQualifiedName(fullObjectName, tokenText);
                    }
                    Location[] locations = GetSqlObjectDefinition(
                                tokenText,
                                schemaName,
                                sqlObjectTypesFromQuickInfo[tokenType.ToLowerInvariant()]
                            );
                    DefinitionResult result = new DefinitionResult
                    {
                        IsErrorResult = this.error,
                        Message = this.errorMessage,
                        Locations = locations
                    };
                    return result;
                }
                else
                {
                    // If a type was found but is not in sqlScriptGettersFromQuickInfo, then the type is not supported
                    return GetDefinitionErrorResult(SR.PeekDefinitionTypeNotSupportedError);
                }
            }
            // no definition found
            return GetDefinitionErrorResult(SR.PeekDefinitionNoResultsError);
        }

        /// <summary>
        /// Script a object using the type extracted from declarationItem
        /// </summary>
        /// <param name="declarationItem">The Declaration object that matched with the selected token</param>
        /// <param name="tokenText">The text of the selected token</param>
        /// <param name="schemaName">Schema name</param>
        /// <returns></returns>
        internal DefinitionResult GetDefinitionUsingDeclarationType(DeclarationType type, string databaseQualifiedName, string tokenText, string schemaName)
        {
            if (sqlObjectTypes.ContainsKey(type))
            {
                // With SqlLogin authentication, the defaultSchema property throws an Exception when accessed.
                // This workaround ensures that a schema name is present by attempting
                // to get the schema name from the declaration item.
                // If all fails, the default schema name is assumed to be "dbo"
                if ((connectionInfo != null && connectionInfo.ConnectionDetails.AuthenticationType.Equals(Constants.SqlLoginAuthenticationType)) && string.IsNullOrEmpty(schemaName))
                {
                    string fullObjectName = databaseQualifiedName;
                    schemaName = this.GetSchemaFromDatabaseQualifiedName(fullObjectName, tokenText);
                }
                Location[] locations = GetSqlObjectDefinition(
                            tokenText,
                            schemaName,
                            sqlObjectTypes[type]
                        );
                DefinitionResult result = new DefinitionResult
                {
                    IsErrorResult = this.error,
                    Message = this.errorMessage,
                    Locations = locations
                };
                return result;
            }
            // If a type was found but is not in sqlScriptGetters, then the type is not supported
            return GetDefinitionErrorResult(SR.PeekDefinitionTypeNotSupportedError);
        }

        /// <summary>
        /// Script a object using SMO and write to a file.
        /// </summary>
        /// <param name="sqlScriptGetter">Function that returns the SMO scripts for an object</param>
        /// <param name="objectName">SQL object name</param>
        /// <param name="schemaName">Schema name or null</param>
        /// <param name="objectType">Type of SQL object</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetSqlObjectDefinition(
                string objectName,
                string schemaName,
                string objectType)
        {
            // script file destination
            string tempFileName = (schemaName != null) ? Path.Combine(this.tempPath, string.Format("{0}.{1}.sql", schemaName, objectName))
                                                : Path.Combine(this.tempPath, string.Format("{0}.sql", objectName));

            SmoScriptingOperation operation = InitScriptOperation(objectName, schemaName, objectType);
            operation.Execute();
            string script = operation.ScriptText;

            bool objectFound = false;
            int createStatementLineNumber = 0;

            File.WriteAllText(tempFileName, script);
            string[] lines = File.ReadAllLines(tempFileName);
            int lineCount = 0;
            string createSyntax = null;
            if (objectScriptMap.ContainsKey(objectType.ToLower()))
            {
                createSyntax = string.Format("CREATE");
                foreach (string line in lines)
                {
                    if (LineContainsObject(line, objectName, createSyntax))
                    {
                        createStatementLineNumber = lineCount;
                        objectFound = true;
                        break;
                    }
                    lineCount++;
                }
            }
            if (objectFound)
            {
                Location[] locations = GetLocationFromFile(tempFileName, createStatementLineNumber);
                return locations;
            }
            else
            {
                this.error = true;
                this.errorMessage = SR.PeekDefinitionNoResultsError;
                return null;
            }
        }

        #region Helper Methods
        /// <summary>
        /// Return schema name from the full name of the database. If schema is missing return dbo as schema name.
        /// </summary>
        /// <param name="fullObjectName"> The full database qualified name(database.schema.object)</param>
        /// <param name="objectName"> Object name</param>
        /// <returns>Schema name</returns>
        internal string GetSchemaFromDatabaseQualifiedName(string fullObjectName, string objectName)
        {
            if (!string.IsNullOrEmpty(fullObjectName))
            {
                string[] tokens = fullObjectName.Split('.');
                for (int i = tokens.Length - 1; i > 0; i--)
                {
                    if (tokens[i].Equals(objectName))
                    {
                        return tokens[i - 1];
                    }
                }
            }
            return "dbo";
        }

        /// <summary>
        /// Convert a file to a location array containing a location object as expected by the extension
        /// </summary>
        internal Location[] GetLocationFromFile(string tempFileName, int lineNumber)
        {
            // Get absolute Uri based on uri format. This works around a dotnetcore URI bug for linux paths.
            if (Path.DirectorySeparatorChar.Equals('/'))
            {
                tempFileName = "file:" + tempFileName;
            }
            else
            {
                tempFileName = new Uri(tempFileName).AbsoluteUri;
            }
            // Create a location array containing the tempFile Uri, as expected by VSCode.
            Location[] locations = new[] 
            {
                    new Location 
                    {
                        Uri = tempFileName,
                        Range = new Range 
                        {
                            Start = new Position { Line = lineNumber, Character = 0},
                            End = new Position { Line = lineNumber + 1, Character = 0}
                        }
                    }
            };
            return locations;
        }

        /// <summary>
        /// Helper method to create definition error result object
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns> DefinitionResult</returns>
        internal DefinitionResult GetDefinitionErrorResult(string errorMessage)
        {
            return new DefinitionResult
            {
                IsErrorResult = true,
                Message = errorMessage,
                Locations = null
            };
        }

        /// <summary>
        /// Return full object name(database.schema.objectName) from the quickInfo text("type database.schema.objectName")
        /// </summary>
        /// <param name="quickInfoText">QuickInfo Text for this token</param>
        /// <param name="tokenText">Token Text</param>
        /// <param name="caseSensitivity">StringComparison enum</param>
        /// <returns></returns>
        internal string GetFullObjectNameFromQuickInfo(string quickInfoText, string tokenText, StringComparison caseSensitivity)
        {
            if (string.IsNullOrEmpty(quickInfoText) || string.IsNullOrEmpty(tokenText))
            {
                return null;
            }
            // extract full object name from quickInfo text
            string[] tokens = quickInfoText.Split(' ');
            List<string> tokenList = tokens.Where(el => el.IndexOf(tokenText, caseSensitivity) >= 0).ToList();
            return (tokenList?.Count() > 0) ? tokenList[0] : null;
        }

        /// <summary>
        /// Return token type from the quickInfo text("type database.schema.objectName")
        /// </summary>
        /// <param name="quickInfoText">QuickInfo Text for this token</param>
        /// <param name="tokenText"Token Text></param>
        /// <param name="caseSensitivity">StringComparison enum</param>
        /// <returns></returns>
        internal string GetTokenTypeFromQuickInfo(string quickInfoText, string tokenText, StringComparison caseSensitivity)
        {
            if (string.IsNullOrEmpty(quickInfoText) || string.IsNullOrEmpty(tokenText))
            {
                return null;
            }
            // extract string denoting the token type from quickInfo text
            string[] tokens = quickInfoText.Split(' ');
            List<int> indexList = tokens.Select((s, i) => new { i, s }).Where(el => (el.s).IndexOf(tokenText, caseSensitivity) >= 0).Select(el => el.i).ToList();
            return (indexList?.Count() > 0) ? String.Join(" ", tokens.Take(indexList[0])) : null;
        }


        /// <summary>
        /// Wrapper method that calls Resolver.GetQuickInfo
        /// </summary>
        internal string GetQuickInfoForToken(ParseResult parseResult, int parserLine, int parserColumn, IMetadataDisplayInfoProvider metadataDisplayInfoProvider)
        {
            if (parseResult == null || metadataDisplayInfoProvider == null)
            {
                return null;
            }
            Babel.CodeObjectQuickInfo quickInfo = Resolver.GetQuickInfo(
                parseResult, parserLine, parserColumn, metadataDisplayInfoProvider);
            return quickInfo?.Text;
        }

        /// <summary>
        /// Wrapper method that calls Resolver.FindCompletions
        /// </summary>
        /// <param name="parseResult"></param>
        /// <param name="parserLine"></param>
        /// <param name="parserColumn"></param>
        /// <param name="metadataDisplayInfoProvider"></param>
        /// <returns></returns>
        internal IEnumerable<Declaration> GetCompletionsForToken(ParseResult parseResult, int parserLine, int parserColumn, IMetadataDisplayInfoProvider metadataDisplayInfoProvider)
        {
            if (parseResult == null || metadataDisplayInfoProvider == null)
            {
                return null;
            }
            return Resolver.FindCompletions(
                parseResult, parserLine, parserColumn, metadataDisplayInfoProvider);
        }

        /// <summary>
        /// Wrapper method that calls Resolver.FindCompletions
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="schemaName"></param>
        /// <param name="objectType"></param>
        /// <param name="tempFileName"></param>
        /// <returns></returns>
        internal SmoScriptingOperation InitScriptOperation(string objectName, string schemaName, string objectType)
        {            
            // object that has to be scripted
            ScriptingObject scriptingObject = new ScriptingObject 
            {
                Name = objectName,
                Schema = schemaName,
                Type = objectType
            };

            // scripting options
            ScriptOptions options = new ScriptOptions 
            {
                ScriptCreateDrop = "ScriptCreate",
                TypeOfDataToScript = "SchemaOnly",
                ScriptStatistics = "ScriptStatsNone",
                ScriptExtendedProperties = false,
                ScriptUseDatabase = false,
                IncludeIfNotExists = false,
                GenerateScriptForDependentObjects = false,
                IncludeDescriptiveHeaders = false,
                ScriptCheckConstraints = false,
                ScriptChangeTracking = false,
                ScriptDataCompressionOptions = false,
                ScriptForeignKeys = false,
                ScriptFullTextIndexes = false,
                ScriptIndexes = false,
                ScriptPrimaryKeys = false,
                ScriptTriggers = false,
                UniqueKeys = false

            };

            List<ScriptingObject> objectList = new List<ScriptingObject>();
            objectList.Add(scriptingObject);

            // create parameters for the scripting operation

            ScriptingParams parameters = new ScriptingParams 
            {
                ConnectionString = ConnectionService.BuildConnectionString(this.connectionInfo.ConnectionDetails),
                ScriptingObjects = objectList,
                ScriptOptions = options,
                ScriptDestination = "ToEditor"
            };

            return new ScriptAsScriptingOperation(parameters, DataSource);
        }

        internal bool LineContainsObject(string line, string objectName, string createSyntax)
        {
            if (line.IndexOf(createSyntax, StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >=0)
            {
                return true;
            }
            return false;
        }

        internal static class ScriptingGlobals
        {
            /// <summary>
            /// Left delimiter for an named object
            /// </summary>
            public const char LeftDelimiter = '[';

            /// <summary>
            /// right delimiter for a named object
            /// </summary>
            public const char RightDelimiter = ']';
        }

        internal static class ScriptingUtils
        {
            /// <summary>
            /// Quote the name of a given sql object.
            /// </summary>
            /// <param name="sqlObject">object</param>
            /// <returns>quoted object name</returns>
            internal static string QuoteObjectName(string sqlObject)
            {
                return QuoteObjectName(sqlObject, ']');
            }

            /// <summary>
            /// Quotes the name of a given sql object
            /// </summary>
            /// <param name="sqlObject">object</param>
            /// <param name="quote">quote to use</param>
            /// <returns></returns>
            internal static string QuoteObjectName(string sqlObject, char quote)
            {
                int len = sqlObject.Length;
                StringBuilder result = new StringBuilder(sqlObject.Length);
                for (int i = 0; i < len; i++)
                {
                    if (sqlObject[i] == quote)
                    {
                        result.Append(quote);
                    }
                    result.Append(sqlObject[i]);
                }
                return result.ToString();
            }
        }

        internal static string SelectAllValuesFromTransmissionQueue(Urn urn)
        {
            string script = string.Empty;
            StringBuilder selectQuery = new StringBuilder();

            /*
             SELECT TOP *, casted_message_body =
              CASE MESSAGE_TYPE_NAME WHEN 'X'
                 THEN CAST(MESSAGE_BODY AS NVARCHAR(MAX))
                 ELSE MESSAGE_BODY
              END
              FROM [new].[sys].[transmission_queue]
             */
            selectQuery.Append("SELECT TOP (1000) ");
            selectQuery.Append("*, casted_message_body = \r\nCASE message_type_name WHEN 'X' \r\n  THEN CAST(message_body AS NVARCHAR(MAX)) \r\n  ELSE message_body \r\nEND \r\n");

            // from clause
            selectQuery.Append("FROM ");
            Urn dbUrn = urn;

            // database
            while (dbUrn.Parent != null && dbUrn.Type != "Database")
            {
                dbUrn = dbUrn.Parent;
            }
            selectQuery.AppendFormat("{0}{1}{2}",
                            ScriptingGlobals.LeftDelimiter,
                            ScriptingUtils.QuoteObjectName(dbUrn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                            ScriptingGlobals.RightDelimiter);
            //SYS
            selectQuery.AppendFormat(".{0}sys{1}",
                                     ScriptingGlobals.LeftDelimiter,
                                     ScriptingGlobals.RightDelimiter);
            //TRANSMISSION QUEUE
            selectQuery.AppendFormat(".{0}transmission_queue{1}",
                                      ScriptingGlobals.LeftDelimiter,
                                      ScriptingGlobals.RightDelimiter);

            script = selectQuery.ToString();
            return script;
        }

        internal static string SelectAllValues(Urn urn)
        {
            string script = string.Empty;
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT TOP (1000) ");
            selectQuery.Append("*, casted_message_body = \r\nCASE message_type_name WHEN 'X' \r\n  THEN CAST(message_body AS NVARCHAR(MAX)) \r\n  ELSE message_body \r\nEND \r\n");

            // from clause
            selectQuery.Append("FROM ");
            Urn dbUrn = urn;

            // database
            while (dbUrn.Parent != null && dbUrn.Type != "Database")
            {
                dbUrn = dbUrn.Parent;
            }
            selectQuery.AppendFormat("{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                ScriptingUtils.QuoteObjectName(dbUrn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);
            // schema
            selectQuery.AppendFormat(".{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                ScriptingUtils.QuoteObjectName(urn.GetAttribute("Schema"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);
            // object
            selectQuery.AppendFormat(".{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                ScriptingUtils.QuoteObjectName(urn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);

            //Adding no lock in the end.
            selectQuery.AppendFormat(" WITH(NOLOCK)");

            script = selectQuery.ToString();
            return script;
        }

        internal DataTable GetColumnNames(Server server, Urn urn, bool isDw)
        {
            List<string> filterExpressions = new List<string>();
            if (server.Version.Major >= 10)
            {
                // We don't have to include sparce columns as all the sparce columns data.
                // Can be obtain from column set columns.
                filterExpressions.Add("@IsSparse=0");
            }

            // Check if we're called for EDIT for SQL2016+/Sterling+.
            // We need to omit temporal columns if such are present on this table.
            if (server.Version.Major >= 13 || (DatabaseEngineType.SqlAzureDatabase == server.DatabaseEngineType && server.Version.Major >= 12))
            {
                // We're called in order to generate a list of columns for EDIT TOP N rows.
                // Don't return auto-generated, auto-populated, read-only temporal columns.
                filterExpressions.Add("@GeneratedAlwaysType=0");
            }

            // Check if we're called for SQL2017/Sterling+.
            // We need to omit graph internal columns if such are present on this table.
            if (server.Version.Major >= 14 || (DatabaseEngineType.SqlAzureDatabase == server.DatabaseEngineType && !isDw))
            {
                // from Smo.GraphType:
                // 0 = None
                // 1 = GraphId
                // 2 = GraphIdComputed
                // 3 = GraphFromId
                // 4 = GraphFromObjId
                // 5 = GraphFromIdComputed
                // 6 = GraphToId
                // 7 = GraphToObjId
                // 8 = GraphToIdComputed
                //
                // We only want to show types 0, 2, 5, and 8:
                filterExpressions.Add("(@GraphType=0 or @GraphType=2 or @GraphType=5 or @GraphType=8)");
            }
            
            Request request = new Request();
            // If we have any filters on the columns, add them.
            if (filterExpressions.Count > 0)
            {
                request.Urn = String.Format("{0}/Column[{1}]", urn.ToString(), string.Join(" and ", filterExpressions.ToArray()));
            }
            else
            {
                request.Urn = String.Format("{0}/Column", urn.ToString());
            }

            request.Fields = new String[] { "Name" };

            // get the columns in the order they were created
            OrderBy order = new OrderBy();
            order.Dir = OrderBy.Direction.Asc;
            order.Field = "ID";
            request.OrderByList = new OrderBy[] { order };

            Enumerator en = new Enumerator();

            // perform the query.
            DataTable dt = null;
            EnumResult result = en.Process(server.ConnectionContext, request);

            if (result.Type == ResultType.DataTable)
            {
                dt = result;
            }
            else
            {
                dt = ((DataSet)result).Tables[0];
            }
            return dt;    
        }

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
            
            var kustoDataSource = dataSource as KustoDataSource;
            var functionInfo = kustoDataSource.GetFunctionInfo(functionName);

            if (functionInfo == null)
            {
                return string.Empty;
            }

            var alterCommand = new StringBuilder();

            alterCommand.Append(".alter function with ");
            alterCommand.Append($"(folder = \"{functionInfo.Folder}\", docstring = \"{functionInfo.DocString}\", skipvalidation = \"false\" ) ");
            alterCommand.Append($"{functionInfo.Name}{functionInfo.Parameters} ");
            alterCommand.Append($"{functionInfo.Body}");

            return alterCommand.ToString();
        }

        #endregion
    }
}