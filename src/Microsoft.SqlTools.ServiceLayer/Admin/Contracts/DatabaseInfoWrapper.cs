//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts 
{
    /// <summary>
    /// Wrapper object for the database info
    /// </summary>
    public class DatabaseInfoWrapper
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Owner of the database
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Collation of the database
        /// </summary>
        public string Collation { get; set; }

        /// <summary>
        /// Recovery Model of the database
        /// </summary>
        public string RecoveryModel { get; set; }

        /// <summary>
        /// State or Health of the database
        /// </summary>
        public string DatabaseState { get; set; }

        /// <summary>
        /// Last backup date
        /// </summary>
        public string LastBackupDate { get; set; }

        /// <summary>
        /// Last log backup date
        /// </summary>
        public string LastLogBackupDate { get; set; }
    }
}