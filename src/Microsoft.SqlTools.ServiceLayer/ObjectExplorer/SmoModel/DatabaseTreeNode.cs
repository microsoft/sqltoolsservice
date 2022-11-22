﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    internal partial class DatabaseTreeNode
    {
        public DatabaseTreeNode(ServerNode serverNode, string databaseName): this()
        {
            Parent = serverNode;
            NodeValue = databaseName;
            var db = new Database(serverNode.GetContextAs<SmoQueryContext>().Server, this.NodeValue);
            db.Refresh();
            // If we got this far, the connection is valid. However, it's possible
            // the user connected directly to the master of a readable secondary
            // In that case, the name in the connection string won't be found in sys.databases
            // We detect that here and fall back to master
            if (db.State == SqlSmoState.Creating && !IsDWGen3(db))
            {
                Logger.Information($"Database {databaseName} is in Creating state after initialization, defaulting to master for Object Explorer connections. This is expected when connecting to an Availability Group readable secondary");
                db = new Database(serverNode.GetContextAs<SmoQueryContext>().Server, "master");
                db.Refresh();
            }
            CacheInfoFromModel(db);
        }

        /// <summary>
        /// Initializes the context and sets its ValidFor property
        /// </summary>
        protected override void EnsureContextInitialized()
        {
            if (context == null)
            {
                base.EnsureContextInitialized();
                var db = SmoObject as Database;
                if (db != null)
                {
                    context.Database = db;
                }
                context.ValidFor = ServerVersionHelper.GetValidForFlag(context.SqlServerType, db);
            }
        }

        protected override void PopulateChildren(bool refresh, string name, CancellationToken cancellationToken)
        {
            var smoQueryContext = this.GetContextAs<SmoQueryContext>();
            if (IsAccessible(smoQueryContext))
            {
                base.PopulateChildren(refresh, name, cancellationToken);
            }
            else
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    // Write error message if it wasn't already set during IsAccessible check
                    ErrorMessage = string.Format(CultureInfo.InvariantCulture, SR.DatabaseNotAccessible, this.NodeValue);
                }
            }
        }

        public bool IsAccessible(SmoQueryContext context)
        {
            try
            {
                return context?.Database == null || context.Database.IsAccessible;
            }
            catch (Exception ex)
            {
                // IsAccessible is not set of DW Gen3 so exception is expected in this case
                if (IsDWGen3(context?.Database))
                {
                    return true;
                }
                else
                {
                    var error = string.Format(CultureInfo.InvariantCulture, "Failed to get IsAccessible. error:{0} inner:{1} stacktrace:{2}",
                        ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                    Logger.Write(TraceEventType.Error, error);
                    ErrorMessage = ex.Message;
                    return false;
                }

            }
        }

        private bool IsDWGen3(Database db)
        {
            return db != null
                && db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse
                && db.ServerVersion.Major == 12;
        }
    }
}
