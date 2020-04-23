//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Telemetry
{
    class EventProperty
    {
        public static readonly string PropertyPrefix = "SQL.SSMS.AutoParameterization.";

        public static readonly string TOTAL_PARAMETERS = "TOTAL_PARAMETER_COUNT";
        public static readonly string TOTAL_ERRORS = "TOTAL_ERROR_COUNT";
        public static readonly string SCRIPT_CHAR_LENGTH = "SCRIPT_CHAR_LENGTH";
        public static readonly string EXCEPTION_TYPE = "EXCEPTION_TYPE";
        public static readonly string EXCEPTION_MESSAGE = "EXCEPTION_MESSAGE";
        public static readonly string STACK_TRACE = "STACK_TRACE";
        public static readonly string LITERAL_SQL_DATA_TYPE = "LITERAL_SQL_DATA_TYPE";
        public static readonly string LITERAL_CSHARP_DATA_TYPE = "LITERAL_CSHARP_DATA_TYPE";
        public static readonly string PARSE_SUCCESSFUL = "PARSE_SUCCESSFUL";

        internal EventProperty(string name, string value)
        {
            Name = PropertyPrefix + name;
            Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }
}
