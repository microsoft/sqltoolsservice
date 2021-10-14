//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class DropdownProperties : ComponentPropertiesBase
    {
        public string Value { get; set; }

        public string[] Values { get; set; }
    }
}