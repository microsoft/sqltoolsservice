using Microsoft.SqlServer.Dac;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    public class DacFxUtils
    {
        public Dictionary<string, TraceListener> _addedListernerDict;
        public DacFxUtils()
        {
            _addedListernerDict = new Dictionary<string, TraceListener>();
        }

        /// <summary>
        /// Set the diagnostics logging 
        /// </summary>
        public void SetUpDiagnosticsLogging(string diagPath)
        {
            if (!string.IsNullOrEmpty(diagPath))
            {
                try
                {
                    FileStream logStream = new FileStream(diagPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    var textWriteTracerListener = new TextWriterTraceListener(logStream, diagPath);

                    // Check if other diagnostic trace listeners exists, remove them from the DacService tracer collection 
                    // This will avoid the same diagnostic listener being added to other operations, and avoid in adding one operation's logs to other log file
                    for (int i = 0; i < DacServices.DiagnosticTrace.Listeners.Count; i++)
                    {
                        if (DacServices.DiagnosticTrace.Listeners[i].GetType().Name == "TextWriterTraceListener")
                        {
                            _addedListernerDict.Add(DacServices.DiagnosticTrace.Listeners[i].Name, DacServices.DiagnosticTrace.Listeners[i]);
                            DacServices.DiagnosticTrace.Listeners.RemoveAt(i);
                        }
                    }

                    // Add the Diagnostic Trace listener to the dac service for the current operation
                    DacServices.DiagnosticTrace.Listeners.Add(textWriteTracerListener);
                    DacServices.DiagnosticTrace.Switch.Level = SourceLevels.Verbose;
                }
                finally
                {

                }
            }
        }

        /// <summary>
        /// Removing the diagnostic tracer from the listener collections
        /// </summary>
        /// <param name="path"></param>
        public void RemoveDiagnosticListener(string path)
        {
            // Remove the listener from the dictionary, if not remove it from the traceListenerCollections
            if (!_addedListernerDict.Remove(path))
            {
                for (int i = 0; i < DacServices.DiagnosticTrace.Listeners.Count; i++)
                {
                    if (DacServices.DiagnosticTrace.Listeners[i].Name == path)
                    {
                        DacServices.DiagnosticTrace.Listeners.RemoveAt(i);
                        break;
                    }
                }
            }
        }
         
    }
}
