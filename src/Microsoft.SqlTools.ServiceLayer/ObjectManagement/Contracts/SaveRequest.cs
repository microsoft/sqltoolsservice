//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class SaveObjectRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The context id.
        /// </summary>
        public string ContextId { get; set; }
        /// <summary>
        /// The object information.
        /// </summary>
        public JToken Object { get; set; }
    }

    public class SaveObjectRequestResponse
    {
        /// <summary>
        /// The task id associated with the save operation.
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// The task failure message when the save operation completes unsuccessfully.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class SaveObjectRequest
    {
        public static readonly RequestType<SaveObjectRequestParams, SaveObjectRequestResponse> Type = RequestType<SaveObjectRequestParams, SaveObjectRequestResponse>.Create("objectManagement/save");
    }
}