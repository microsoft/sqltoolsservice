//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Abstraction for receiving progress messages from schema compare publish operations.
    ///
    /// VSCode/ADS implements this to forward messages to SqlTask.AddMessage().
    /// SSMS implements this to update its own progress status bar/output window.
    ///
    /// Assign to SchemaComparePublishDatabaseChangesOperation.ProgressHandler
    /// before calling Execute().
    /// </summary>
    public interface ISchemaCompareProgressHandler
    {
        /// <summary>
        /// Called for each progress message during publish.
        /// </summary>
        /// <param name="message">The progress message text.</param>
        /// <param name="isError">True if the message is an error; false for informational/warning.</param>
        void OnProgress(string message, bool isError = false);
    }
}
