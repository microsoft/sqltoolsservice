//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

# if NET // TODO: remove; tracked in SqlToolsCore as Task #1254787

using Dac = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;

namespace Microsoft.SqlTools.SqlCore.TableDesigner
{
    public static class TableDesignerMetadata
    {
        public static bool IsNode(this Dac.TableDesigner tableDesigner)
        {
            return tableDesigner.TableViewModel.IsNode;
        }

        public static bool IsEdge(this Dac.TableDesigner tableDesigner)
        {
            return tableDesigner.TableViewModel.IsEdge;
        }

        public static bool IsSystemVersioned(this Dac.TableDesigner tableDesigner)
        {
            return tableDesigner.TableViewModel.IsSystemVersioningEnabled;
        }
    }

}

#endif