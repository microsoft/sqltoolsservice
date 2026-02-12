//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Represents an update to a SQL code analysis rule configuration
    /// </summary>
    public class SqlCodeAnalysisRuleUpdate
    {
        /// <summary>
        /// The unique identifier for the rule (e.g., "SR0001")
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// The configured severity of the rule (Error, Warning, None)
        /// </summary>
        public string Severity { get; set; }
    }

    /// <summary>
    /// Parameters for updating code analysis rules configuration
    /// </summary>
    public class UpdateCodeAnalysisRulesParams
    {
        /// <summary>
        /// URI of the SQL project file
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// The list of rule updates to apply
        /// </summary>
        public SqlCodeAnalysisRuleUpdate[] Rules { get; set; }
    }

    /// <summary>
    /// Request to update code analysis rules for a SQL project
    /// </summary>
    class UpdateCodeAnalysisRulesRequest
    {
        public static readonly RequestType<UpdateCodeAnalysisRulesParams, ResultStatus> Type =
            RequestType<UpdateCodeAnalysisRulesParams, ResultStatus>.Create("dacfx/updateCodeAnalysisRules");
    }
}
