//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common
{
    /// <summary>
    /// If the ranges have an overlap, they're compared equal, otherwise this class follows regular comparison semantics.
    /// </summary>
    internal class SqlIntegerRangeOverlapComparer : IComparer<SqlIntegerRange>
    {
        public int Compare(SqlIntegerRange x, SqlIntegerRange y)
        {
            int result;
            if (x.Begin < y.Begin)
            {
                if (x.End >= y.Begin)
                {
                    result = 0;
                }
                else
                {
                    result = -1;
                }
            }
            else if (x.Begin > y.Begin)
            {
                result = y.End >= x.Begin ? 0 : 1;
            }
            else
            {
                Debug.Assert(x.Begin == y.Begin, "Begin should compare equal.");
                result = 0;
            }
            return result;
        }
    }
}
