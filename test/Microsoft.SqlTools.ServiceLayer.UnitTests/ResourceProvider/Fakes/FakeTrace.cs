//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    [Exportable(typeof(ITrace), "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeTrace")
]
    public class FakeTrace : ITrace
    {
        private readonly List<string> _traces = new List<string>(); 
        public bool TraceEvent(TraceEventType eventType, int traceId, string message, params object[] args)
        {
            _traces.Add(message);
            return true;
        }

        public bool TraceException(TraceEventType eventType, int traceId, Exception exception, string message, int lineNumber = 0,
            string fileName = "", string memberName = "")
        {
            return true;
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> Traces
        {
            get
            {
                return _traces;
            }
        }

        public IExportableMetadata Metadata { get; set; }
        public ExportableStatus Status { get; }
    }
}
