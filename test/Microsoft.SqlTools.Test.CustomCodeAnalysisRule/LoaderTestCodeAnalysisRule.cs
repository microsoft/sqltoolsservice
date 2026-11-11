//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Microsoft.SqlTools.Test.CustomCodeAnalysisRule
{
    [ExportCodeAnalysisRule(
        RuleId,
        "Loader test rule",
        Description = "Validates trusted custom rule loading.",
        Category = "Test",
        RuleScope = SqlRuleScope.Model)]
    public sealed class LoaderTestCodeAnalysisRule : SqlCodeAnalysisRule
    {
        public const string RuleId = "Contoso.SR9000";

        public override IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext ruleExecutionContext)
        {
            return Array.Empty<SqlRuleProblem>();
        }
    }
}
