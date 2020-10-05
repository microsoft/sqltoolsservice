//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Range = Microsoft.Kusto.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that reqires knowledge of
    /// the language to perfom, such as definitions, intellisense, etc.
    /// </summary>
    public static class DiagnosticsHelper
    {
        /// <summary>
        /// Send the diagnostic results back to the host application
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="semanticMarkers"></param>
        /// <param name="eventContext"></param>
        internal static async Task PublishScriptDiagnostics(
            ScriptFile scriptFile,
            ScriptFileMarker[] semanticMarkers,
            EventContext eventContext)
        {
            var allMarkers = scriptFile.SyntaxMarkers != null
                    ? scriptFile.SyntaxMarkers.Concat(semanticMarkers)
                    : semanticMarkers;

            // Always send syntax and semantic errors.  We want to 
            // make sure no out-of-date markers are being displayed.
            await eventContext.SendEvent(
                PublishDiagnosticsNotification.Type,
                new PublishDiagnosticsNotification
                {
                    Uri = scriptFile.ClientUri,
                    Diagnostics =
                       allMarkers
                            .Select(GetDiagnosticFromMarker)
                            .ToArray()
                });
        }

        /// <summary>
        /// Send the diagnostic results back to the host application
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="semanticMarkers"></param>
        /// <param name="eventContext"></param>
        internal static async Task ClearScriptDiagnostics(
            string uri,
            EventContext eventContext)
        {
            Validate.IsNotNullOrEmptyString(nameof(uri), uri);
            Validate.IsNotNull(nameof(eventContext), eventContext);
            // Always send syntax and semantic errors.  We want to 
            // make sure no out-of-date markers are being displayed.
            await eventContext.SendEvent(
                PublishDiagnosticsNotification.Type,
                new PublishDiagnosticsNotification
                {
                    Uri = uri,
                    Diagnostics = Array.Empty<Diagnostic>()
                });
        }

        /// <summary>
        /// Convert a ScriptFileMarker to a Diagnostic that is Language Service compatible
        /// </summary>
        /// <param name="scriptFileMarker"></param>
        /// <returns></returns>
        private static Diagnostic GetDiagnosticFromMarker(ScriptFileMarker scriptFileMarker)
        {
            return new Diagnostic
            {
                Severity = MapDiagnosticSeverity(scriptFileMarker.Level),
                Message = scriptFileMarker.Message,
                Range = new Range
                {
                    Start = new Position
                    {
                        Line = scriptFileMarker.ScriptRegion.StartLineNumber - 1,
                        Character = scriptFileMarker.ScriptRegion.StartColumnNumber - 1
                    },
                    End = new Position
                    {
                        Line = scriptFileMarker.ScriptRegion.EndLineNumber - 1,
                        Character = scriptFileMarker.ScriptRegion.EndColumnNumber - 1
                    }
                }
            };
        }

        /// <summary>
        /// Map ScriptFileMarker severity to Diagnostic severity
        /// </summary>
        /// <param name="markerLevel"></param>        
        private static DiagnosticSeverity MapDiagnosticSeverity(ScriptFileMarkerLevel markerLevel)
        {
            switch (markerLevel)
            {
                case ScriptFileMarkerLevel.Error:
                    return DiagnosticSeverity.Error;

                case ScriptFileMarkerLevel.Warning:
                    return DiagnosticSeverity.Warning;

                case ScriptFileMarkerLevel.Information:
                    return DiagnosticSeverity.Information;

                default:
                    return DiagnosticSeverity.Error;
            }
        }
    }
}
