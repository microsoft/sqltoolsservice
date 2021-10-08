//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// XEL stream request parameters
    /// </summary>
    public class XELStreamParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string SessionName { get; set; }

        public ProfilerSessionTemplate Template { get; set; }
    }

    public class XELStreamResult{}

    /// <summary>
    /// XEL stream request type
    /// </summary>
    public class XELStreamRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<XELStreamParams, XELStreamResult> Type =
            RequestType<XELStreamParams, XELStreamResult>.Create("profiler/createstream");
    }
}
