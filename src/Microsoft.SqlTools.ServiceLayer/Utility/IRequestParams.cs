//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public interface IRequestParams
    {
        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        string OwnerUri { get; set; }
    }
}
