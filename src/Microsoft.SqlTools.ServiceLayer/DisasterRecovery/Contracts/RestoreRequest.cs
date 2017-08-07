//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Restore response
    /// </summary>
    public class RestoreResponse
    {
        /// <summary>
        /// Indicates if the restore task created successfully 
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// The task id assosiated witht the restore operation
        /// </summary>
        public string TaskId { get; set; }


        /// <summary>
        /// Errors occurred while creating the restore operation task
        /// </summary>
        public string ErrorMessage { get; set; }
    }

   

    public class RestoreRequest
    {
        public static readonly
            RequestType<RestoreParams, RestoreResponse> Type =
                RequestType<RestoreParams, RestoreResponse>.Create("disasterrecovery/restore");
    }

   
}
