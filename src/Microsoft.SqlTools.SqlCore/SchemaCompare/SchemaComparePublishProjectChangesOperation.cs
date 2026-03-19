//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Threading;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare publish project changes operation.
    /// </summary>
    public class SchemaComparePublishProjectChangesOperation : SchemaComparePublishChangesOperation
    {
        /// <summary>
        /// Gets the parameters for the publish project changes operation.
        /// </summary>
        public SchemaComparePublishProjectChangesParams Parameters { get; }

        /// <summary>
        /// Result from DacFx PublishChangesToProject call.
        /// </summary>
        public SchemaComparePublishProjectResult PublishResult { get; set; }

        /// <summary>
        /// Initializes a new publish project changes operation with parameters and comparison result.
        /// </summary>
        public SchemaComparePublishProjectChangesOperation(SchemaComparePublishProjectChangesParams parameters, SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
        }

        /// <summary>
        /// Executes the publish operation, applying schema changes to the target SQL project.
        /// </summary>
        public override void Execute()
        {
            if (CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(CancellationToken);
            }

            try
            {
                PublishResult = ComparisonResult.PublishChangesToProject(Parameters.TargetProjectPath, Parameters.TargetFolderStructure);

                if (!PublishResult.Success)
                {
                    ErrorMessage = PublishResult.ErrorMessage;
                    throw new DacServicesException(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare publish project changes operation {0} failed with exception {1}", OperationId, e.Message));
                throw;
            }
        }
    }
}
