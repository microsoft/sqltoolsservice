//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The view model of check constraint
    /// </summary>
    public class CheckConstraintViewModel : ObjectViewModelBase
    {
        public InputBoxProperties Expression { get; set; } = new InputBoxProperties();
        public CheckBoxProperties Enabled { get; set; } = new CheckBoxProperties();
    }
}