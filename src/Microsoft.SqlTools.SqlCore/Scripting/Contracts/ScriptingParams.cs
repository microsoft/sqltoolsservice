//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.SqlCore.Scripting.Contracts
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

        /// <summary>
        /// Gets or sets the connection owner URI
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The script operation
        /// </summary>
        public ScriptingOperationType Operation { get; set; } = ScriptingOperationType.Create;

        /// <summary>
        /// Gets or sets whether to return the operation ID immediately and deliver the script via the complete event.
        /// When true, the scripting/script request returns immediately with an operation ID,
        /// and the script content is delivered later via the scripting/scriptComplete event.
        /// When false (default), the request waits for completion and returns both operation ID and script together.
        /// </summary>
        public bool ReturnScriptAsEvent { get; set; }

    }
}
