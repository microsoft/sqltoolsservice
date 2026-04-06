//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Abstraction for handling generated scripts from schema compare operations.
    /// VSCode implements this to feed scripts to SqlTask;
    /// SSMS implements this to return scripts directly to the caller.
    /// </summary>
    public interface ISchemaCompareScriptHandler
    {
        /// <summary>
        /// Handle a generated deployment script.
        /// </summary>
        void OnScriptGenerated(string script);

        /// <summary>
        /// Handle a generated master database script (for Azure SQL DB).
        /// </summary>
        void OnMasterScriptGenerated(string masterScript);
    }
}
