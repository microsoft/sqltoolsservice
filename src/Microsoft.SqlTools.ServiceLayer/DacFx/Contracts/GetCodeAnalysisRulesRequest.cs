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
    /// Parameters for a get code analysis rules request.
    /// </summary>
    public class GetCodeAnalysisRulesParams
    {
        /// <summary>
        /// Location of the .sqlproj file, either a <c>file://</c> URI or a plain OS path.
        /// When provided, custom rules from NuGet-referenced analyzer packages are included.
        /// When absent, only built-in DacFx rules are returned (backward-compatible).
        /// </summary>
        public string ProjectUri { get; set; }
    }

    /// <summary>
    /// Represents a SQL code analysis rule with its metadata
    /// </summary>
    public class CodeAnalysisRuleInfo
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

        /// <summary>
        /// True for built-in DacFx rules; false for custom rules from NuGet packages.
        /// </summary>
        public bool IsBuiltIn { get; set; }
    }

    /// <summary>
    /// Result containing the list of available code analysis rules
    /// </summary>
    public class GetCodeAnalysisRulesResult : ResultStatus
    {
        /// <summary>
        /// The list of available code analysis rules
        /// </summary>
        public CodeAnalysisRuleInfo[] Rules { get; set; }

        /// <summary>
        /// Non-fatal warning to surface in the UI (e.g., project not restored, DLL load failure, ID conflict).
        /// </summary>
        public string Warning { get; set; }

        /// <summary>
        /// True when running a package restore would resolve the reported warning, either because the
        /// project has never been restored or because a referenced package no longer matches what was
        /// restored. Lets the client offer a restore action without parsing the localized warning text.
        /// </summary>
        public bool RestoreRequired { get; set; }
    }

    /// <summary>
    /// Request to get all available built-in SQL code analysis rules from DacFx
    /// </summary>
    class GetCodeAnalysisRulesRequest
    {
        public static readonly RequestType<GetCodeAnalysisRulesParams, GetCodeAnalysisRulesResult> Type =
            RequestType<GetCodeAnalysisRulesParams, GetCodeAnalysisRulesResult>.Create("dacfx/getCodeAnalysisRules");
    }
}
