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
    /// Parameters for getting code analysis rules
    /// </summary>
    public class GetCodeAnalysisRulesParams
    {
        /// <summary>
        /// URI of the SQL project file
        /// </summary>
        public string ProjectUri { get; set; }
    }

    /// <summary>
    /// Represents a SQL code analysis rule
    /// </summary>
    public class SqlCodeAnalysisRule
    {
        /// <summary>
        /// The unique identifier for the rule (e.g., "SR0001")
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// The short identifier for the rule (e.g., "SR0001")
        /// </summary>
        public string ShortRuleId { get; set; }

        /// <summary>
        /// The display name of the rule
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The description of the rule
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The category of the rule (e.g., "Design", "Performance", "Naming")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The default severity of the rule (Error, Warning, None)
        /// </summary>
        public string DefaultSeverity { get; set; }

        /// <summary>
        /// The current configured severity of the rule
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// The scope of the rule (Element or Model)
        /// </summary>
        public string RuleScope { get; set; }
    }

    /// <summary>
    /// Result containing code analysis rules
    /// </summary>
    public class GetCodeAnalysisRulesResult : ResultStatus
    {
        /// <summary>
        /// The list of available code analysis rules
        /// </summary>
        public SqlCodeAnalysisRule[] Rules { get; set; }
    }

    /// <summary>
    /// Request to get code analysis rules for a SQL project
    /// </summary>
    class GetCodeAnalysisRulesRequest
    {
        public static readonly RequestType<GetCodeAnalysisRulesParams, GetCodeAnalysisRulesResult> Type =
            RequestType<GetCodeAnalysisRulesParams, GetCodeAnalysisRulesResult>.Create("dacfx/getCodeAnalysisRules");
    }
}
