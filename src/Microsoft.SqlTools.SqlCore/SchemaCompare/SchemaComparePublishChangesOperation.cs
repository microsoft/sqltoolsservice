//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic base class for schema compare publish operations.
    /// </summary>
    public abstract class SchemaComparePublishChangesOperation : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this publish operation.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// The schema comparison result to publish.
        /// </summary>
        public SchemaComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// The error message if the publish operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        protected CancellationToken CancellationToken { get { return cancellation.Token; } }

        protected readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        /// <summary>
        /// Initializes a new publish changes operation with the given comparison result.
        /// </summary>
        public SchemaComparePublishChangesOperation(SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull(nameof(comparisonResult), comparisonResult);
            ComparisonResult = comparisonResult;
        }

        /// <summary>
        /// Executes the publish changes operation.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Cancels the running publish operation.
        /// </summary>
        public void Cancel()
        {
            cancellation.Cancel();
        }

        /// <summary>
        /// Disposes the operation and cancels any pending work.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
