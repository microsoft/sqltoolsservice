//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.DataProtocol.Hosting.Utility
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> AsSingleItemEnumerable<T>(this T obj)
        {
            yield return obj;
        }
    }
}
