//------------------------------------------------------------------------------
// <copyright file="BeforeResolveChangesEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// EventArgs class for the BeforeResolveChanges event defined
    /// in the model updating service. Used to requested the list
    /// of files that should be processed after an edit is performed
    /// </summary>
    internal class BeforeResolveChangesEventArgs : EventArgs
    {
        public IEnumerable<string> AdditionalFilesToProcess { get; set; }
    }
}
