//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.Azure.Core.Extensibility
{
    /// <summary>
    /// An interface to be implemented by any class that needs to be exportable
    /// </summary>
    public interface IExportable : IComposableService
    {
        /// <summary>
        /// The metadata assigned to the exportable
        /// </summary>
        IExportableMetadata Metadata
        {
            set; get;
        }
        
        /// <summary>
        /// Returns the status of the exportable
        /// </summary>
        ExportableStatus Status
        {
            get;
        }

    }
}
