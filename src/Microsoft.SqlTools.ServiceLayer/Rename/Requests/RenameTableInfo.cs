//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    /// <summary>
    /// Property class for Metadata Service
    /// </summary>
    public class RenameTableInfo
    {
        public string TableName { get; set; }
        public string OldName { get; set; }
        public string Schema { get; set; }
        public string Id { get; set; }
        public string OwnerUri { get; set; }
        public string Database { get; set; }

    }
}