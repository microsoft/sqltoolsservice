//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    public class StartProfilingParams : GeneralRequestDetails
    {
         public string TemplateName {
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
        /// 
        /// </summary>
        public string SessionId { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class StartProfilingRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly
            RequestType<StartProfilingParams, StartProfilingResult> Type =
            RequestType<StartProfilingParams, StartProfilingResult>.Create("profiler/startprofiling");
    }
}
