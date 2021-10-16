//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.View.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// View related operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        void DeleteView(SqlView view);
    }
}
