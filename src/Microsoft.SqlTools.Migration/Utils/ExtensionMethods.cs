//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Migration.Logins.Contracts.Exceptions;

namespace Microsoft.SqlTools.Migration.Utils
{
    internal static class ExtensionMethods
    {
        public static void AddExceptions(this IDictionary<string, IEnumerable<LoginMigrationException>> exceptionMap1, IDictionary<string, IEnumerable<LoginMigrationException>> exceptionMap2)
        {
            if (exceptionMap1 is null || exceptionMap2 is null)
            {
                return;
            }

            foreach (var keyValuePair2 in exceptionMap2)
            {
                // If the dictionary already contains the key then merge them
                if (exceptionMap1.TryGetValue(keyValuePair2.Key, out _))
                {
                    foreach (var value in keyValuePair2.Value)
                    {
                        exceptionMap1[keyValuePair2.Key].Append(value);
                    }
                    continue;
                }
                exceptionMap1.Add(keyValuePair2);
            }
        }
    }
}
