//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public sealed class CellUpdate
    {
        public DbColumn Column { get; set; }

        public object Value { get; set; }
    }
}
