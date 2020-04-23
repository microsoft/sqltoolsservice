//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition
{
    internal static class AutoParameterizationConstants
    {
        public const string ComponentName = "AUTO_PARAMETERIZATION: ";
        public const string AutoParameterizationEnabledDuringExecution = "AUTO_PARAMETERIZATION_ENABLED";
        public const string FinishedQueryTransformationExecution = "FINISHED_QUERY_TRANSFORMATION";
        public const string ScriptGenerationSucceededExecution = "SCRIPT_GENERATION_SUCCEEDED";
        public const string LiteralExpression = "Expression Type: Literal";
        public const string UnaryExpression = "Expression Type: Unary Expression";
        public const string ParenthesisExpression = "Expression Type: Parenthesis Expression";
    }
}
