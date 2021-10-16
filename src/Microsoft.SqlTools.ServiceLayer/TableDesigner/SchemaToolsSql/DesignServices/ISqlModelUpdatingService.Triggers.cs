//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.Triggers.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Trigger related operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        void CreateDmlTrigger(SqlTable table, string triggerName);
        void DeleteDmlTrigger(SqlDmlTrigger sqlTrigger);
    }
}
