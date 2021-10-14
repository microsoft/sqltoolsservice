//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class TableDesignerValidationError
    {
        public string Message { get; set; }

        public object Property { get; set; }
    }
}