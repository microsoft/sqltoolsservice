//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    internal partial class DatabaseTreeNode
    {
        public DatabaseTreeNode(ServerNode serverNode, string databaseName): this()
        {
            Parent = serverNode;
            NodeValue = databaseName;

            CacheInfoFromModel(DataSourceFactory.CreateDatabaseMetadata(serverNode.ObjectMetadata, databaseName));
        }

        /// <summary>
        /// Initializes the context and sets its ValidFor property 
        /// </summary>
        protected override void EnsureContextInitialized()
        {
        }

        protected override void PopulateChildren(bool refresh, string name, CancellationToken cancellationToken)
        {
            var smoQueryContext = this.GetContextAs<QueryContext>();
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

        public bool IsAccessible(QueryContext context)
        {
            try
            {
                return context?.Database == null || context.Database.IsAccessible;
            }
            catch (Exception ex)
            {
                var error = string.Format(CultureInfo.InvariantCulture, "Failed to get IsAccessible. error:{0} inner:{1} stacktrace:{2}",
                    ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(TraceEventType.Error, error);
                ErrorMessage = ex.Message;
                return false;
            }
        }
    }
}
