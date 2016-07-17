//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices;
using Microsoft.SqlTools.EditorServices.Session;

namespace Microsoft.SqlTools.LanguageSupport
{
    /// <summary>
    /// Main class for Language Service functionality
    /// </summary>
    public class LanguageService
    {
        /// <summary>
        /// Gets or sets the current SQL Tools context
        /// </summary>
        /// <returns></returns>
        private SqlToolsContext Context { get; set; }

        /// <summary>
        /// Constructor for the Language Service class
        /// </summary>
        /// <param name="context"></param>
        public LanguageService(SqlToolsContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Gets a list of semantic diagnostic marks for the provided script file
        /// </summary>
        /// <param name="scriptFile"></param>
        public ScriptFileMarker[] GetSemanticMarkers(ScriptFile scriptFile)
        {
            // the commented out snippet is an example of how to create a error marker
            // semanticMarkers = new ScriptFileMarker[1];
            // semanticMarkers[0] = new ScriptFileMarker()
            // {
            //     Message = "Error message",
            //     Level = ScriptFileMarkerLevel.Error,
            //     ScriptRegion = new ScriptRegion()
            //     {
            //         File = scriptFile.FilePath,
            //         StartLineNumber = 2,
            //         StartColumnNumber = 2,  
            //         StartOffset = 0,
            //         EndLineNumber = 4,
            //         EndColumnNumber = 10,
            //         EndOffset = 0
            //     }
            // };
            return new ScriptFileMarker[0];
        }
    }
}
