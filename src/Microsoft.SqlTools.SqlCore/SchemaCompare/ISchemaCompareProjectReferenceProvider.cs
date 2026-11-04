//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Supplies the database references and SQLCMD variable values of a SQL project, so a Schema Compare project
    /// endpoint resolves referenced objects and compares [$(Variable)] names by their values, as a build would.
    /// Implemented by the same object as <see cref="ISchemaCompareConnectionProvider"/> when the host can resolve references.
    /// </summary>
    public interface ISchemaCompareProjectReferenceProvider
    {
        /// <summary>
        /// Returns the references of the project at <paramref name="projectFilePath"/>, or null when they are not known.
        /// </summary>
        SchemaCompareProjectReferences GetProjectReferences(string projectFilePath);
    }

    /// <summary>
    /// A SQL project's resolved database references and SQLCMD variable values.
    /// </summary>
    public sealed class SchemaCompareProjectReferences
    {
        public SchemaCompareProjectReferences(IEnumerable<TSqlModelReference> databaseReferences, IReadOnlyDictionary<string, string> sqlCmdVariables)
        {
            DatabaseReferences = (databaseReferences ?? throw new ArgumentNullException(nameof(databaseReferences))).ToArray();
            SqlCmdVariables = sqlCmdVariables ?? throw new ArgumentNullException(nameof(sqlCmdVariables));
        }

        /// <summary>The project's direct references, each ready to load.</summary>
        public TSqlModelReference[] DatabaseReferences { get; }

        /// <summary>SQLCMD variables that have a value, keyed by name without $().</summary>
        public IReadOnlyDictionary<string, string> SqlCmdVariables { get; }
    }
}
