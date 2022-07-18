//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Custom name for view
    /// </summary>
    internal partial class ViewsChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            try
            {
                View view = smoObject as View;
                if (view != null &&
                    IsPropertySupported("LedgerViewType", smoContext, view, CachedSmoProperties) &&
                    view.LedgerViewType == LedgerViewType.LedgerView)
                {
                    return $"{view.Schema}.{view.Name} ({SR.Ledger_LabelPart})";
                }
            }
            catch
            {
                //Ignore the exception and just not change create custom name
            }

            return string.Empty;
        }

        public override string GetNodePathName(object smoObject)
        {
            return ViewCustomNodeHelper.GetPathName(smoObject);
        }
    }

    internal static class ViewCustomNodeHelper
    {
        internal static string GetPathName(object smoObject)
        {
            View view = smoObject as View;
            if (view != null)
            {
                return $"{view.Schema}.{view.Name}";
            }

            return string.Empty;
        }
    }
}
