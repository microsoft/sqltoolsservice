//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    public class DatabaseInfo
    {
        /// <summary>
        /// Gets or sets the options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }

        public DatabaseInfo()
        {
            Options = new Dictionary<string, object>();
        }
        
    }
}
