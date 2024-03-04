//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The information requested by the table designer UI.
    /// </summary>
    [DataContract]
    public class TableDesignerInfo
    {
        [DataMember(Name = "view")]
        public TableDesignerView View { get; set; }
        [DataMember(Name = "viewModel")]
        public TableViewModel ViewModel { get; set; }
        [DataMember(Name = "tableInfo")]
        public TableInfo TableInfo { get; set; }
        [DataMember(Name = "issues")]
        public TableDesignerIssue[] Issues { get; set; }
    }
}