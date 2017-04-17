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
    /// <summary>
    /// Extension methods used by the scripting service.
    /// </summary>
    internal static class ScriptingExtensionMethods
    {
        /// <summary>
        /// Returns a list of ScriptingObject instances for the passed SqlScriptPublishModel instance.
        /// </summary>
        /// <param name="publishModel">The sql script publish model instance.</param>
        /// <returns>The list of scripting objects.</returns>
        public static List<ScriptingObject> GetDatabaseObjects(this SqlScriptPublishModel publishModel)
        {
            List<ScriptingObject> databaseObjects = new List<ScriptingObject>();

            IEnumerable<DatabaseObjectType> objectTypes = publishModel.GetDatabaseObjectTypes();
            foreach (DatabaseObjectType objectType in objectTypes)
            {
                IEnumerable<KeyValuePair<string, string>> databaseObjectsOfType = publishModel.EnumChildrenForDatabaseObjectType(objectType);

                Logger.Write(
                    LogLevel.Verbose,
                    string.Format(
                        "Loaded SMO urn object count {0} for type {1}, urns: {2}",
                        objectType,
                        databaseObjectsOfType.Count(),
                        string.Join(", ", databaseObjectsOfType.Select(p => p.Value))));

                foreach (KeyValuePair<string, string> databaseObjectOfType in databaseObjectsOfType)
                {
                    databaseObjects.Add(new Urn(databaseObjectOfType.Value).ToScriptingObject());
                }
            }

            return databaseObjects;
        }

        /// <summary>
        /// Returns true if the passed exception or any inner exception is an OperationCanceledException instance.
        /// </summary>
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

        /// <summary>
        /// Creates a SMO Urn instance based on the passed ScriptingObject instance.
        /// </summary>
        /// <param name="scriptingObject">The scripting object instance.</param>
        /// <param name="database">The name of the database referenced by the Urn.</param>
        /// <returns>The Urn instance.</returns>
        public static Urn ToUrn(this ScriptingObject scriptingObject, string database)
        {
            if (scriptingObject == null)
            {
                throw new ArgumentNullException("scriptingObjectc");
            }

            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (string.IsNullOrWhiteSpace(scriptingObject.Name))
            {
                throw new ArgumentException("Property scriptingObject.Name must have a value", "scriptingObject");
            }

            if (string.IsNullOrWhiteSpace(scriptingObject.Type))
            {
                throw new ArgumentException("Property scriptingObject.Type must have a value", "scriptingObject");
            }

            // Leaving the server name blank will automatically match whatever the server SMO is running against.
            string urn = string.Format(
                "Server/Database[@Name='{0}']/{1}[@Name='{2}' {3}]",
                database,
                scriptingObject.Type,
                scriptingObject.Name,
                scriptingObject.Schema != null ? string.Format("and @Schema = '{0}'", scriptingObject.Schema) : string.Empty);

            return new Urn(urn);
        }

        /// <summary>
        /// Creates a ScriptingObject instance based on the passed SMO Urn instance.
        /// </summary>
        /// <param name="urn">The urn instance.</param>
        /// <returns>The scripting object instance.</returns>
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
