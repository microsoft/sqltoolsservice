/**************************************************************
*  Copyright (C) Microsoft Corporation. All rights reserved.  *
**************************************************************/

using System;
using System.Diagnostics;

namespace Microsoft.Data.Tools.Components.Diagnostics
{
    internal interface ISqlTraceTelemetryProvider
    {
        void PostEvent(TraceEventType eventType, SqlTraceId traceId, Exception exception,
            int lineNumber = 0, string fileName = "", string memberName = "");
    }
}
