//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.SqlCore.IntelliSense
{
    /// <summary>
    /// Another database that a SQL project references, as exposed to IntelliSense for three-part names
    /// such as [$(OtherDb)].[dbo].[Table].
    /// </summary>
    public sealed class ProjectReferencedDatabase
    {
        /// <param name="names">
        /// Names the database can be referred to by in the project's scripts: the reference's literal database name,
        /// or its SQLCMD variable written as $(Variable) together with the variable's value.
        /// </param>
        /// <param name="objects">Top level objects loaded from the reference.</param>
        public ProjectReferencedDatabase(IEnumerable<string> names, IEnumerable<TSqlObject> objects)
        {
            Names = (names ?? throw new ArgumentNullException(nameof(names)))
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Objects = (objects ?? throw new ArgumentNullException(nameof(objects))).ToArray();
        }

        public IReadOnlyList<string> Names { get; }

        public IReadOnlyList<TSqlObject> Objects { get; }
    }
}
