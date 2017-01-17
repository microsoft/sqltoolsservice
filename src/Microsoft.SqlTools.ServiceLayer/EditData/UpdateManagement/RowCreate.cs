//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public sealed class RowCreate : RowEditBase
    {
        private List<CellUpdate> newCells;

        public RowCreate(long rowId, ResultSet associatedResultSet) : base(rowId, associatedResultSet)
        {
            newCells = new List<CellUpdate>();
        }

        public override string GetScript()
        {
            throw new NotImplementedException();
        }

        public override string SetCell(int columnId, string newValue)
        {
            throw new NotImplementedException();
        }
    }
}
