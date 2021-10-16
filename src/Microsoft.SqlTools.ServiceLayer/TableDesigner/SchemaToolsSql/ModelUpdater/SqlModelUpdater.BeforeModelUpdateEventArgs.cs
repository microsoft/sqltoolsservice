//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdater.BeforeModelUpdateEventArgs.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed class BeforeModelUpdateEventArgs : EventArgs
    {
        public BeforeModelUpdateEventArgs(IEnumerable<SqlScriptUpdateInfo> updates)
        {
            this.Updates = updates;
        }

        public IEnumerable<SqlScriptUpdateInfo> Updates { get; private set; }
    }
}
