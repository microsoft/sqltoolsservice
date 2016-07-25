//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices;
using Microsoft.SqlTools.EditorServices.Session;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using System.Collections.Generic;

namespace Microsoft.SqlTools.LanguageSupport
{
    /// <summary>
    /// Main class for Language Service functionality
    /// </summary>
    public class LanguageService
    {
        /// <summary>
        /// The cached parse result from previous incremental parse
        /// </summary>
        private ParseResult prevParseResult;

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
            // parse current SQL file contents to retrieve a list of errors
            ParseOptions parseOptions = new ParseOptions();
            ParseResult parseResult = Parser.IncrementalParse(
                scriptFile.Contents,
                prevParseResult,
                parseOptions);

            // save previous result for next incremental parse
            this.prevParseResult = parseResult;

            // build a list of SQL script file markers from the errors
            List<ScriptFileMarker> markers = new List<ScriptFileMarker>();
            foreach (var error in parseResult.Errors)
            {
                markers.Add(new ScriptFileMarker()
                {
                    Message = error.Message,
                    Level = ScriptFileMarkerLevel.Error,
                    ScriptRegion = new ScriptRegion()
                    {
                        File = scriptFile.FilePath,
                        StartLineNumber = error.Start.LineNumber,
                        StartColumnNumber = error.Start.ColumnNumber,  
                        StartOffset = 0,
                        EndLineNumber = error.End.LineNumber,
                        EndColumnNumber = error.End.ColumnNumber,
                        EndOffset = 0
                    }
                });
            }
            
            return markers.ToArray();
        }
    }
}
