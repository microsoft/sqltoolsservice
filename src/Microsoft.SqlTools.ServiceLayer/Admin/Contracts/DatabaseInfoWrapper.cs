//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts 
{
    public class DatabaseInfoWrapper
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Collation { get; set; }
        public string RecoveryModel { get; set; }
        public string DatabaseState { get; set; }
    }
}