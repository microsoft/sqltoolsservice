﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using System.Data;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;
using Microsoft.SqlTools.SqlCore.Scripting;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    internal partial class Scripter
    {
        private bool error;
        private string errorMessage;
        private ServerConnection serverConnection;
        private ConnectionInfo connectionInfo;
        private Database database;
        private string tempPath;

        // Dictionary that holds the object name (as appears on the TSQL create statement)
        private Dictionary<DeclarationType, string> sqlObjectTypes = new Dictionary<DeclarationType, string>();

        private Dictionary<string, string> sqlObjectTypesFromQuickInfo = new Dictionary<string, string>();

        private Dictionary<DatabaseEngineEdition, string> targetDatabaseEngineEditionMap = new Dictionary<DatabaseEngineEdition, string>();

        private Dictionary<int, string> serverVersionMap = new Dictionary<int, string>();

        private Dictionary<string, string> objectScriptMap = new Dictionary<string, string>();

        internal Scripter() { }

        /// <summary>
        /// Initialize a Peek Definition helper object
        /// </summary>
        /// <param name="serverConnection">SMO Server connection</param>
        internal Scripter(ServerConnection serverConnection, ConnectionInfo connInfo)
        {
            this.serverConnection = serverConnection;
            this.connectionInfo = connInfo;
            this.tempPath = FileUtilities.GetPeekDefinitionTempFolder();
            Initialize();
        }

        internal Database Database
        {
            get
            {
                if (this.database == null)
                {
                    if (this.serverConnection != null && !string.IsNullOrEmpty(this.serverConnection.DatabaseName))
                    {
                        try
                        {
                            // Reuse existing connection
                            Server server = new Server(this.serverConnection);
                            // The default database name is the database name of the server connection
                            string dbName = this.serverConnection.DatabaseName;
                            if (this.connectionInfo != null)
                            {
                                // If there is a query DbConnection, use that connection to get the database name
                                // This is preferred since it has the most current database name (in case of database switching)
                                DbConnection connection;
                                if (connectionInfo.TryGetConnection(ConnectionType.Query, out connection))
                                {
                                    if (!string.IsNullOrEmpty(connection.Database))
                                    {
                                        dbName = connection.Database;
                                    }
                                }
                            }
                            this.database = new Database(server, dbName);
                            this.database.Refresh();
                        }
                        catch (ConnectionFailureException cfe)
                        {
                            Logger.Error("Exception at PeekDefinition Database.get() : " + cfe.Message);
                            this.error = true;
                            this.errorMessage = (connectionInfo != null && connectionInfo.IsCloud) ? SR.PeekDefinitionAzureError(cfe.Message) : SR.PeekDefinitionError(cfe.Message);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Exception at PeekDefinition Database.get() : " + ex.Message);
                            this.error = true;
                            this.errorMessage = SR.PeekDefinitionError(ex.Message);
                            return null;
                        }
                    }
                }
                return this.database;
            }
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
                    if (this.Database == null)
                    {
                        return GetDefinitionErrorResult(SR.PeekDefinitionDatabaseError);
                    }
                    StringComparison caseSensitivity = this.Database.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
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
            if (this.Database == null)
            {
                return GetDefinitionErrorResult(SR.PeekDefinitionDatabaseError);
            }
            StringComparison caseSensitivity = this.Database.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            string tokenType = GetTokenTypeFromQuickInfo(quickInfoText, tokenText, caseSensitivity);
            if (tokenType != null)
            {
                if (sqlObjectTypesFromQuickInfo.TryGetValue(tokenType.ToLowerInvariant(), out string sqlObjectType))
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
                                sqlObjectType);
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
            if (sqlObjectTypes.TryGetValue(type, out string sqlObjectType))
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
                            sqlObjectType);
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
            if (objectScriptMap.ContainsKey(objectType.ToLower(System.Globalization.CultureInfo.InvariantCulture)))
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
            return (tokenList?.Count > 0) ? tokenList[0] : null;
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
            return (indexList?.Count > 0) ? String.Join(" ", tokens.Take(indexList[0])) : null;
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
                TargetDatabaseEngineEdition = GetTargetDatabaseEngineEdition(),
                TargetDatabaseEngineType = GetTargetDatabaseEngineType(),
                ScriptCompatibilityOption = GetScriptCompatibilityOption(),
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

            return new ScriptAsScriptingOperation(parameters, serverConnection);
        }

        internal string GetTargetDatabaseEngineEdition()
        {
            DatabaseEngineEdition dbEngineEdition = this.serverConnection.DatabaseEngineEdition;
            string dbEngineEditionString;
            targetDatabaseEngineEditionMap.TryGetValue(dbEngineEdition, out dbEngineEditionString);
            return (dbEngineEditionString != null) ? dbEngineEditionString : "SqlServerEnterpriseEdition";
        }

        internal string GetScriptCompatibilityOption()
        {
            int serverVersion = this.serverConnection.ServerVersion.Major;
            string dbEngineTypeString = serverVersionMap[serverVersion];
            return (dbEngineTypeString != null) ? dbEngineTypeString : "Script140Compat";
        }

        internal string GetTargetDatabaseEngineType()
        {
            return connectionInfo.IsCloud ? "SqlAzure" : "SingleInstance";
        }

        internal bool LineContainsObject(string line, string objectName, string createSyntax)
        {
            if (line.IndexOf(createSyntax, StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}