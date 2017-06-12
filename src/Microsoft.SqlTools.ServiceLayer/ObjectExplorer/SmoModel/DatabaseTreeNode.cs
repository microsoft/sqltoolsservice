//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Globalization;
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
            Database db = new Database(serverNode.GetContextAs<SmoQueryContext>().Server, this.NodeValue);
            db.Refresh();
            CacheInfoFromModel(db);
        }

        /// <summary>
        /// Initializes the context and ensures that 
        /// </summary>
        protected override void EnsureContextInitialized()
        {
            if (context == null)
            {
                base.EnsureContextInitialized();
                Database db = SmoObject as Database;
                if (context != null && db != null)
                {
                    context.Database = db;
                }
            }
        }

        protected override void PopulateChildren(bool refresh, string name = null)
        {
            SmoQueryContext context = this.GetContextAs<SmoQueryContext>();
            if (IsAccessible(context))
            {
                base.PopulateChildren(refresh, name);
            }
            else
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    // Write error message if it wasn't already set during IsAccessible check
                    ErrorMessage = string.Format(CultureInfo.InvariantCulture, SR.DatabaseNotAccessible, context.Database.Name);
                }
            }
        }

        public bool IsAccessible(SmoQueryContext context)
        {
            try
            {
                if (context == null || context.Database == null)
                {
                    return true;
                }
                return context.Database.IsAccessible;
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed to get IsAccessible. error:{0} inner:{1} stacktrace:{2}",
                    ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(LogLevel.Error, error);
                ErrorMessage = ex.Message;
                return false;
            }
        }
    }
}
