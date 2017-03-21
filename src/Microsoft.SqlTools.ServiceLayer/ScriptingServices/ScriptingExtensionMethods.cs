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
        public static ScriptingObject ToScriptingObject(this Urn urn)
        {
            return new ScriptingObject
            {
                Type = urn.Type,
                Schema = urn.GetAttribute("Schema"),
                Name = urn.GetAttribute("Name"),
            };
        }
    }
}
