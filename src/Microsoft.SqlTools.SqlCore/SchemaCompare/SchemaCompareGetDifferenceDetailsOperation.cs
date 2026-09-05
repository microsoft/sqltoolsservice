//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Materializes scripts and children for one top-level schema difference on demand.
    /// </summary>
    public class SchemaCompareGetDifferenceDetailsOperation
    {
        private readonly SchemaCompareDifferenceDetailsParams parameters;
        private readonly SchemaComparisonResult comparisonResult;

        public SchemaCompareGetDifferenceDetailsOperation(
            SchemaCompareDifferenceDetailsParams parameters,
            SchemaComparisonResult comparisonResult)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.comparisonResult = comparisonResult ?? throw new ArgumentNullException(nameof(comparisonResult));
        }

        public SchemaCompareDifferenceDetailsResult Execute()
        {
            var differences = this.comparisonResult.Differences?.ToList();
            if (differences == null || this.parameters.DifferenceIndex < 0 || this.parameters.DifferenceIndex >= differences.Count)
            {
                return new SchemaCompareDifferenceDetailsResult
                {
                    Success = false,
                    ErrorMessage = "The requested schema difference was not found."
                };
            }

            DiffEntry difference = SchemaCompareUtils.CreateDiffEntry(
                differences[this.parameters.DifferenceIndex],
                null,
                this.comparisonResult);

            return new SchemaCompareDifferenceDetailsResult
            {
                Success = true,
                Difference = difference
            };
        }
    }
}
