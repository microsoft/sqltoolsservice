//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare publish database changes operation.
    /// </summary>
    public class SchemaComparePublishDatabaseChangesOperation : SchemaComparePublishChangesOperation
    {
        /// <summary>
        /// Gets the parameters for the publish database changes operation.
        /// </summary>
        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        /// <summary>
        /// The result of publishing changes to the database.
        /// </summary>
        public SchemaComparePublishResult PublishResult { get; set; }

        /// <summary>
        /// Initializes a new publish database changes operation with parameters and comparison result.
        /// </summary>
        public SchemaComparePublishDatabaseChangesOperation(SchemaComparePublishDatabaseChangesParams parameters, SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
        }

        /// <summary>
        /// Executes the publish operation, applying schema changes to the target database.
        /// </summary>
        public override void Execute()
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                PublishResult = ComparisonResult.PublishChangesToDatabase(CancellationToken);

                if (!PublishResult.Success)
                {
                    ErrorMessage = String.Join(Environment.NewLine, this.PublishResult.Errors.Where(x => x.MessageType == DacMessageType.Error || x.MessageType == DacMessageType.Warning));
                    throw new DacServicesException(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare publish database changes operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
        }
    }
}
