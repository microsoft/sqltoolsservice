//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.Extensibility
{

    public static class IEnumerableExt
    {
        public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}
