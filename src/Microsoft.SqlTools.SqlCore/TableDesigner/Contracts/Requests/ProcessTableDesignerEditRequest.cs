//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    [DataContract]
    public class ProcessTableDesignerEditRequestParams : GeneralRequestDetails
    {
        [DataMember(Name = "tableInfo")]
        public TableInfo TableInfo { get; set; }
        [DataMember(Name = "tableChangeInfo")]
        public TableDesignerChangeInfo TableChangeInfo { get; set; }
    }

    [DataContract]
    public class ProcessTableDesignerEditResponse
    {
        [DataMember(Name = "viewModel")]
        public TableViewModel ViewModel { get; set; }
        [DataMember(Name = "view")]
        public TableDesignerView View { get; set; }
        [DataMember(Name = "isValid")]
        public bool IsValid { get; set; }
        [DataMember(Name = "issues")]
        public TableDesignerIssue[] Issues { get; set; }
        [DataMember(Name = "metadata")]
        public Dictionary<string, string> Metadata { get; set; }
        [DataMember(Name = "inputValidationError")]
        public string InputValidationError { get; set; }
    }

    /// <summary>
    /// The service request to process the changes made in the table designer.
    /// </summary>
    public class ProcessTableDesignerEditRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<ProcessTableDesignerEditRequestParams, ProcessTableDesignerEditResponse> Type = RequestType<ProcessTableDesignerEditRequestParams, ProcessTableDesignerEditResponse>.Create("tabledesigner/processedit");
    }
}
