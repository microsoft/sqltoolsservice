//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    public class EditCell : DbCellValue
    {
        public EditCell(DbCellValue dbCellValue, bool isDirty)
        {
            IsDirty = isDirty;


            DisplayValue = dbCellValue.DisplayValue;
            IsNull = dbCellValue.IsNull;
            RawObject = dbCellValue.RawObject;
        }

        public bool IsDirty { get; set; }
    }
}