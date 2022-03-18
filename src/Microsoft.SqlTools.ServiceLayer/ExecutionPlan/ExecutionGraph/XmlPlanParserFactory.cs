//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph
{
    internal static class XmlPlanParserFactory
    {
        public static XmlPlanParser GetParser(Type type)
        {
            while (true)
            {
                switch (type.Name)
                {
                    case "RelOpType":
                        return RelOpTypeParser.Instance;
                    
                    case "BaseStmtInfoType":
                        return StatementParser.Instance;

                    case "RelOpBaseType":
                        return RelOpBaseTypeParser.Instance;

                    case "FilterType":
                        return FilterTypeParser.Instance;

                    case "MergeType":
                        return MergeTypeParser.Instance;

                    case "StmtCursorType":
                        return CursorStatementParser.Instance;

                    case "CursorPlanTypeOperation":
                        return CursorOperationParser.Instance;

                    case "StmtBlockType":
                    case "QueryPlanType":
                    case "CursorPlanType":
                    case "ReceivePlanTypeOperation":
                    case "StmtCondTypeThen":
                    case "StmtCondTypeElse":
                        return XmlPlanHierarchyParser.Instance;

                    case "StmtCondTypeCondition":
                        return ConditionParser.Instance;

                    case "FunctionType":
                        return FunctionTypeParser.Instance;

                    case "IndexScanType":
                    case "CreateIndexType":
                        return IndexOpTypeParser.Instance;

                    case "Object":
                        return null;

                    default:
                        type = type.BaseType;
                        break;
                }
            }
        }
    }
}
