//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices
{
    public static class ScriptingExtensionMethods
    {
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
