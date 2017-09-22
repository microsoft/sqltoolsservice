//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Start Profiling request parameters
    /// </summary>
    public class StartProfilingParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string TemplateName 
        {
            get
            {
                return GetOptionValue<string>("templateName");
            }
            set
            {
                SetOptionValue("templateName", value);
            }
        }
    }

    public class StartProfilingResult
    {
        /// <summary>
        /// Session ID that was started
        /// </summary>
        public string SessionId { get; set; }

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class StartProfilingRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<StartProfilingParams, StartProfilingResult> Type =
            RequestType<StartProfilingParams, StartProfilingResult>.Create("profiler/start");
    }
}
