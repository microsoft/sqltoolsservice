//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.IntelliSense;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.References
{
    /// <summary>
    /// Loads a project's resolved database references into its IntelliSense model.
    /// </summary>
    internal static class ProjectReferenceLoader
    {
        /// <summary>Object types IntelliSense offers for another database.</summary>
        private static readonly ModelTypeClass[] OtherDatabaseObjectTypes =
        {
            ModelSchema.Schema,
            ModelSchema.Table,
            ModelSchema.View,
            ModelSchema.Procedure,
            ModelSchema.ScalarFunction,
            ModelSchema.TableValuedFunction,
            ModelSchema.DataType,
            ModelSchema.TableType,
        };

        /// <summary>
        /// Replaces the references loaded into <paramref name="model"/> with <paramref name="resolved"/> and refreshes
        /// <paramref name="provider"/>: same-database and master references extend the project database, and other-database
        /// references become databases IntelliSense can complete three-part names from.
        /// </summary>
        /// <returns>A message for each reference that could not be loaded.</returns>
        /// <remarks>
        /// Call on a model that isn't published yet, or from the project's <see cref="ProjectHost"/> queue and then
        /// recreate the binder.
        /// </remarks>
        internal static IReadOnlyList<string> ApplyReferences(TSqlModel model, TSqlModelMetadataProvider provider, ResolvedProjectReferences resolved)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(resolved);

            foreach (TSqlModelReference existing in model.References.ToList())
            {
                model.RemoveReference(existing);
            }

            var failures = new List<string>();
            var referencedDatabases = new List<ProjectReferencedDatabase>();
            foreach (ResolvedDatabaseReference reference in resolved.References)
            {
                if (reference.ModelReference == null)
                {
                    failures.Add($"{reference.Name}: {reference.Message}");
                    continue;
                }

                try
                {
                    model.AddReference(reference.ModelReference);
                }
                catch (Exception ex) when (ex is DacModelException || ex is ArgumentException)
                {
                    // For example a corrupt dacpac, a missing script, or the same file referenced twice.
                    failures.Add($"{reference.Name}: {ex.Message}");
                    continue;
                }

                if (reference.DatabaseNames.Count > 0)
                {
                    referencedDatabases.Add(new ProjectReferencedDatabase(
                        reference.DatabaseNames,
                        model.GetReferencedObjects(reference.ModelReference, OtherDatabaseObjectTypes)));
                }
            }

            provider.RefreshReferences(referencedDatabases);
            return failures;
        }
    }
}
