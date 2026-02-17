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
    /// Represents a SQL code analysis rule with its metadata
    /// </summary>
    public class SqlCodeAnalysisRule
    {
        /// <summary>
        /// The full rule identifier (e.g., "Microsoft.Rules.Data.SR0001")
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// The short rule identifier (e.g., "SR0001")
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
        /// The severity of the rule (Error, Warning, None)
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// The scope of the rule (Element or Model)
        /// </summary>
        public string RuleScope { get; set; }
    }

    /// <summary>
    /// Result containing the list of available code analysis rules
    /// </summary>
    public class GetCodeAnalysisRulesResult : ResultStatus
    {
        /// <summary>
        /// The list of available code analysis rules
        /// </summary>
        public SqlCodeAnalysisRule[] Rules { get; set; }
    }

    /// <summary>
    /// Request to get all available built-in SQL code analysis rules from DacFx
    /// </summary>
    class GetCodeAnalysisRulesRequest
    {
        public static readonly RequestType<object, GetCodeAnalysisRulesResult> Type =
            RequestType<object, GetCodeAnalysisRulesResult>.Create("dacfx/getCodeAnalysisRules");
    }
}
