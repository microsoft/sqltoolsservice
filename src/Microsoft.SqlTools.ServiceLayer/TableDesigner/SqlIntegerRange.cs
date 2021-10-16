using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common
{
    /// <summary>
    /// If the ranges have an overlap, they're compared equal, otherwise this class follows regular comparison semantics.
    /// </summary>
    internal struct SqlIntegerRange
    {
        private readonly int _begin;
        private readonly int _end;

        public int Begin { get { return _begin; } }
        public int End { get { return _end; } }

        public SqlIntegerRange(
            int begin,
            int end)
        {
            if (end < begin)
            {
                throw ExceptionFactory.CreateArgumentException(SqlCommonResource.IntegerRange_EndLessThanBegin);
            }
            _begin = begin;
            _end = end;
        }

    }
}
