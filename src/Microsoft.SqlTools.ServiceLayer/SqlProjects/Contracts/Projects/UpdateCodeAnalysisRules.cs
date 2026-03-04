//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Represents a single rule override to apply to a SQL project.
    /// </summary>
    public class CodeAnalysisRuleOverride
    {
        /// <summary>
        /// The rule identifier, using the short ID format (e.g., "SR0001")
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// The desired severity: "Error", "Warning", or "Disabled"
        /// </summary>
        public string Severity { get; set; }
    }

    /// <summary>
    /// Parameters for updating code analysis rule settings in a .sqlproj file.
    /// </summary>
    public class UpdateCodeAnalysisRulesParams : SqlProjectParams
    {
        /// <summary>
        /// The full list of rules and their desired severities.
        /// Rules with the default severity (Warning) are omitted from the project file;
        /// only Error and Disabled overrides are written.
        /// Pass an empty array to reset rules to DacFx defaults (removes SqlCodeAnalysisRules property).
        /// When null, existing rule overrides in the project file are preserved.
        /// </summary>
        public CodeAnalysisRuleOverride[] Rules { get; set; }

        /// <summary>
        /// Whether to enable SQL code analysis during build (&lt;RunSqlCodeAnalysis&gt;).
        /// When null, the existing value in the project file is preserved.
        /// </summary>
        public bool? RunSqlCodeAnalysis { get; set; }
    }

    /// <summary>
    /// Result of an update code analysis rules request.
    /// </summary>
    public class UpdateCodeAnalysisRulesResult : ResultStatus
    {
    }

    /// <summary>
    /// Request to update code analysis rule settings in a .sqlproj file.
    /// </summary>
    public class UpdateCodeAnalysisRulesRequest
    {
        public static readonly RequestType<UpdateCodeAnalysisRulesParams, UpdateCodeAnalysisRulesResult> Type =
            RequestType<UpdateCodeAnalysisRulesParams, UpdateCodeAnalysisRulesResult>.Create("sqlProjects/updateCodeAnalysisRules");
    }
}