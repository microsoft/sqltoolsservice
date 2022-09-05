//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    public enum ChangeType
    {
        TABLE,
        COLUMN

    }
    /// <summary>
    /// Property class for Rename Service
    /// </summary>
    public class RenameTableChangeInfo
    {
        public ChangeType Type { get; set; }
        public string NewName { get; set; }
    }
}