//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Scriptoria.Interfaces;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Copilot
{
    public class CopilotLogger : IScriptoriaTrace
    {
        public TraceSource Source => Logger.TraceSource!;

        public string LogFilePath => Logger.LogFileFullPath;

        public void WriteInfoEvent(int eventId, string message)
        {
            Logger.Information(FormatMessage(eventId, message));
        }

        public void WriteErrorEvent(int eventId, string message)
        {
            Logger.Error(FormatMessage(eventId, message));
        }

        public void WriteVerboseEvent(int eventId, string message)
        {
            Logger.Verbose(FormatMessage(eventId, message));
        }

        public void WriteWarningEvent(int eventId, string message)
        {
            Logger.Warning(FormatMessage(eventId, message));
        }

        public void WriteKernelMethodEvent(TraceEventType eventType, string message, [CallerMemberName] string memberName = "")
        {
            string formattedMessage = $"[{memberName}] {message}";

            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    Logger.Error(formattedMessage);
                    break;
                case TraceEventType.Warning:
                    Logger.Warning(formattedMessage);
                    break;
                case TraceEventType.Information:
                    Logger.Information(formattedMessage);
                    break;
                case TraceEventType.Verbose:
                default:
                    Logger.Verbose(formattedMessage);
                    break;
            }
        }

        private string FormatMessage(int eventId, string message)
        {
            return $"EventId: {eventId}, Message: {message}, ProcessId: {Environment.ProcessId}";
        }
    }
}
