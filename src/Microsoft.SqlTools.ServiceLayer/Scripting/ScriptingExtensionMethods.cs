//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    public static class ScriptingExtensionMethods
    {
        public static List<ScriptingObject> GetDatabaseObjects(this SqlScriptPublishModel publishModel)
        {
            string serverName = null;
            string databaseName = null;
            return GetDatabaseObjects(publishModel, out serverName, out databaseName);
        }

        public static List<ScriptingObject> GetDatabaseObjects(this SqlScriptPublishModel publishModel, out string serverName, out string databaseName)
        {
            List<ScriptingObject> databaseObjects = new List<ScriptingObject>();
            serverName = null;
            databaseName = null;
            bool serverAndDatabaseInitialized = false;

            IEnumerable<DatabaseObjectType> objectTypes = publishModel.GetDatabaseObjectTypes();
            foreach (DatabaseObjectType objectType in objectTypes)
            {
                IEnumerable<KeyValuePair<string, string>> databaseObjectsOfType = publishModel.EnumChildrenForDatabaseObjectType(objectType);

                Logger.Write(
                    LogLevel.Normal,
                    string.Format(
                        "Loaded SMO urn object count {0} for type {1}, urns: {2}",
                        objectType,
                        databaseObjectsOfType.Count(),
                        string.Join(", ", databaseObjectsOfType.Select(p => p.Value))));

                foreach (KeyValuePair<string, string> databaseObjectOfType in databaseObjectsOfType)
                {
                    if (!serverAndDatabaseInitialized)
                    {
                        Urn urn = new Urn(databaseObjectOfType.Value);
                        serverName = urn.GetNameForType("Server");
                        databaseName = urn.GetNameForType("Database");
                        serverAndDatabaseInitialized = true;
                    }

                    databaseObjects.Add(new Urn(databaseObjectOfType.Value).ToScriptingObject());
                }
            }

            return databaseObjects;
        }

        public static bool IsOperationCanceledException(this Exception e)
        {
            Exception current = e;
            while (current != null)
            {
                if (current is OperationCanceledException)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        public static Urn ToUrn(this ScriptingObject scriptingObject, string server, string database)
        {
            if (scriptingObject == null)
            {
                throw new ArgumentNullException("scriptingObjectc");
            }

            if (string.IsNullOrWhiteSpace(server))
            {
                throw new ArgumentException("Parameter must have a value", "server");
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("Parameter must have a value", "database");
            }

            if (string.IsNullOrWhiteSpace(scriptingObject.Name))
            {
                throw new ArgumentException("Property scriptingObject.Name must have a value", "scriptingObject");
            }

            if (string.IsNullOrWhiteSpace(scriptingObject.Type))
            {
                throw new ArgumentException("Property scriptingObject.Type must have a value", "scriptingObject");
            }

            string urn = string.Format(
                "Server[@Name='{0}']/Database[@Name='{1}']/{2}[@Name='{3}' {4}]",
                server,
                database,
                scriptingObject.Type,
                scriptingObject.Name,
                scriptingObject.Schema != null ? string.Format("and @Schema = '{0}'", scriptingObject.Schema) : string.Empty);

            return new Urn(urn);
        }

        public static ScriptingObject ToScriptingObject(this Urn urn)
        {
            if (urn == null)
            {
                throw new ArgumentNullException("urn");
            }

            return new ScriptingObject
            {
                Type = urn.Type,
                Schema = urn.GetAttribute("Schema"),
                Name = urn.GetAttribute("Name"),
            };
        }
    }
}
