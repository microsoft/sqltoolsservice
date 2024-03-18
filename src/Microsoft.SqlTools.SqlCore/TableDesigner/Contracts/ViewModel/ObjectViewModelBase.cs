//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The base class for view model object.
    /// </summary>
    [DataContract]
    public abstract class ObjectViewModelBase
    {
        [DataMember(Name = "name")]
        public InputBoxProperties Name { get; set; } = new InputBoxProperties();
        [DataMember(Name = "description")]
        public InputBoxProperties Description { get; set; } = new InputBoxProperties();
    }
}