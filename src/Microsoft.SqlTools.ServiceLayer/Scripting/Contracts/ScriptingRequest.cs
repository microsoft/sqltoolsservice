//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters for a script request.
    /// </summary>
    public class ScriptingParams
    {
        /// <summary>
        /// Gets or sets the file path used when writing out the script.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets whether scripting to a single file or file per object.
        /// </summary>
        public string ScriptDestination { get; set; }

        /// <summary>
        /// Gets or sets connection string of the target database the scripting operation will run against.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a list of scripting objects to script.
        /// </summary>
        public List<ScriptingObject> ScriptingObjects { get; set; }

        /// <summary>
        /// Gets or sets a list of scripting object which specify the include criteria of objects to script.
        /// </summary>
        public List<ScriptingObject> IncludeObjectCriteria { get; set; }

        /// <summary>
        /// Gets or sets a list of scripting object which specify the exclude criteria of objects to not script.
        /// </summary>
        public List<ScriptingObject> ExcludeObjectCriteria { get; set; }

        /// <summary>
        /// Gets or sets a list of schema name of objects to script.
        /// </summary>
        public List<string> IncludeSchemas { get; set; }

        /// <summary>
        /// Gets or sets a list of schema name of objects to not script.
        /// </summary>
        public List<string> ExcludeSchemas { get; set; }

        /// <summary>
        /// Gets or sets a list of type name of objects to script.
        /// </summary>
        public List<string> IncludeTypes { get; set; }

        /// <summary>
        /// Gets or sets a list of type name of objects to not script
        /// </summary>
        public List<string> ExcludeTypes { get; set; }

        /// <summary>
        /// Gets or sets the scripting options.
        /// </summary>
        public ScriptOptions ScriptOptions { get; set; }
    }

    /// <summary>
    /// Parameters returned from a script request.
    /// </summary>
    public class ScriptingResult
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the scripting request type.
    /// </summary>
    public class ScriptingRequest
    {
        public static readonly RequestType<ScriptingParams, ScriptingResult> Type =
            RequestType<ScriptingParams, ScriptingResult>.Create("scripting/script");
    }
}
