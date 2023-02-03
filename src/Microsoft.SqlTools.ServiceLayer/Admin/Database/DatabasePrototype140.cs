//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SqlServer 2017
    /// </summary>
    internal class DatabasePrototype140 : DatabasePrototype110
    {
        /// <summary>
        /// Database properties for SqlServer 2017 class constructor
        /// </summary>
        public DatabasePrototype140(CDataContainer context)
            : base(context)
        {
        }

        /// <summary>
        /// Whether or not the UI should show File Groups
        /// </summary>
        public override bool HideFileSettings
        {
            get
            {
                return (this.context != null && this.context.Server != null && (this.context.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance || this.context.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand));
            }
        }

        /// <summary>
        /// The recovery model for the database
        /// </summary>
        [Browsable(false)]
        public override RecoveryModel RecoveryModel
        {
            get
            {
                if (this.context != null &&
                    this.context.Server != null &&
                    this.context.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                {
                    return RecoveryModel.Full;
                }
                else
                {
                    return this.currentState.recoveryModel;
                }
            }
            set
            {
                if (this.context != null &&
                    this.context.Server != null &&
                    this.context.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance &&
                    value != RecoveryModel.Full)
                {
                    System.Diagnostics.Debug.Assert(false, "Managed Instance supports only FULL recovery model!");
                    throw new ArgumentException("Managed Instance supports only FULL recovery model!");
                }
                else
                {
                    base.RecoveryModel = value;
                }
            }
        }
    }
}


