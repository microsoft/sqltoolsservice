//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of check constraint
    /// </summary>
    [DataContract]
    public class CheckConstraintViewModel : ObjectViewModelBase
    {
        [DataMember(Name = "expression")]
        public InputBoxProperties Expression { get; set; } = new InputBoxProperties();
        [DataMember(Name = "enabled")]
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();
    }
}