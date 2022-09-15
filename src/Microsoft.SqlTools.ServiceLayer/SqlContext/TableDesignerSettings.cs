//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Contract for receiving table designer settings as part of workspace settings
    /// </summary>
    public class TableDesignerSettings
    {
        /// <summary>
        /// Whether the database model should be preloaded to make the initial launch quicker.
        /// </summary>
        public bool PreloadDatabaseModel { get; set; } = false;
    }
}
