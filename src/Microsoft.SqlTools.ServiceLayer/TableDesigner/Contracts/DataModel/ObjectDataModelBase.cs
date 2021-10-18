//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The base class for data model object.
    /// </summary>
    public abstract class ObjectDataModelBase
    {
        public InputBoxProperties Name { get; set; } = new InputBoxProperties();
    }
}