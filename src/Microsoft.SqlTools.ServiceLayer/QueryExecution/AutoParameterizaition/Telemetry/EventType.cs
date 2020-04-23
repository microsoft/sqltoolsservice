//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Telemetry
{
    public class EventType
    {
        public static readonly EventType USAGE_INFO = new EventType("USAGE_INFO");
        public static readonly EventType CODESENSE_ERRORS_SURFACED_TO_USER = new EventType("ERROR/CODESENSE_ERRORS_SURFACED_TO_USER");
        public static readonly EventType CODESENSE_ERROR = new EventType("ERROR/CODESENSE_ERROR");
        public static readonly EventType EXECUTION_DETAILS = new EventType("EXECUTION_DETAILS");
        public static readonly EventType EXECUTION_ERROR = new EventType("ERROR/EXECUTION_ERROR");

        protected EventType(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
