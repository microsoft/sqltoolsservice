//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Open XEL request parameters
    /// </summary>
    public class OpenXelFileParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string FilePath { get; set; }
    }

    public class OpenXelFileResult
    {
    }

    /// <summary>
    /// Open XEL request type
    /// </summary>
    public class OpenXelFileRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<OpenXelFileParams, OpenXelFileResult> Type =
            RequestType<OpenXelFileParams, OpenXelFileResult>.Create("profiler/openxel");
    }
}
