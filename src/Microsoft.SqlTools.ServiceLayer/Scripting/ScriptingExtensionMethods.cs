//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Gets the status of a scripting operation for the passed scripting event.
        /// </summary>
        /// <param name="e">The scripting event.</param>
        /// <returns>The status.</returns>
        public static string GetStatus(this ScriptEventArgs e)
        {
            Validate.IsNotNull("e", e);

            string status = null;

            if (e.Error != null)
            {
                status = "Error";
            }
            else if (e.Completed)
            {
                status = "Completed";
            }
            else
            {
                status = "Progress";
            }

            return status;
        }

        /// <summary>
        /// Returns a list of ScriptingObject instances for the passed SqlScriptPublishModel instance.
        /// </summary>
        /// <param name="publishModel">The sql script publish model instance.</param>
        /// <returns>The list of scripting objects.</returns>
        public static List<ScriptingObject> GetDatabaseObjects(this SqlScriptPublishModel publishModel)
        {
            Validate.IsNotNull("publishModel", publishModel);

            List<ScriptingObject> databaseObjects = new List<ScriptingObject>();

            IEnumerable<DatabaseObjectType> objectTypes = publishModel.GetDatabaseObjectTypes();
            Logger.Write(
                TraceEventType.Verbose,
                string.Format(
                    "Loaded SMO object type count {0}, types: {1}",
                    objectTypes.Count(),
                    string.Join(", ", objectTypes)));

            foreach (DatabaseObjectType objectType in objectTypes)
            {
                IEnumerable<KeyValuePair<string, string>> databaseObjectsOfType = publishModel.EnumChildrenForDatabaseObjectType(objectType);

                Logger.Write(
                    TraceEventType.Verbose,
                    string.Format(
                        "Loaded SMO urn object count {0} for type {1}, urns: {2}",
                        objectType,
                        databaseObjectsOfType.Count(),
                        string.Join(", ", databaseObjectsOfType.Select(p => p.Value))));

                databaseObjects.AddRange(databaseObjectsOfType.Select(d => new Urn(d.Value).ToScriptingObject()));
            }

            return databaseObjects;
        }

        /// <summary>
        /// Creates a SMO Urn instance based on the passed ScriptingObject instance.
        /// </summary>
        /// <param name="scriptingObject">The scripting object instance.</param>
        /// <param name="database">The name of the database referenced by the Urn.</param>
        /// <returns>The Urn instance.</returns>
        public static Urn ToUrn(this ScriptingObject scriptingObject, string server, string database)
        {
            Validate.IsNotNull("scriptingObject", scriptingObject);
            Validate.IsNotNullOrEmptyString("server", server);
            Validate.IsNotNullOrWhitespaceString("database", database);

            Validate.IsNotNullOrWhitespaceString("scriptingObject.Name", scriptingObject.Name);
            Validate.IsNotNullOrWhitespaceString("scriptingObject.Type", scriptingObject.Type);

            // Leaving the server name blank will automatically match whatever the server SMO is running against.
            string urn = string.Format(
                "Server[@Name='{0}']/Database[@Name='{1}']/{2}[@Name='{3}' {4}]",
                server.ToUpper(),
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
            Validate.IsNotNull("urn", urn);

            return new ScriptingObject
            {
                Type = urn.Type,
                Schema = urn.GetAttribute("Schema"),
                Name = urn.GetAttribute("Name"),
            };
        }
    }
}
