//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Dropdown properties
    /// </summary>
    public class DropdownProperties : ComponentPropertiesBase
    {
        public string Value { get; set; }

        public List<string> Values { get; set; }
    }
}