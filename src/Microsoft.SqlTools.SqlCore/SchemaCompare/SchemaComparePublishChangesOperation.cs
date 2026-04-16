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
    /// Host-agnostic abstract base class for schema compare publish operations
    /// </summary>
    public abstract class SchemaComparePublishChangesOperation : IDisposable
    {
        public string OperationId { get; protected set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        /// Raised when DacFx reports progress during publish.
        /// Consumers should subscribe before calling Execute().
        /// </summary>
        public event EventHandler<SchemaCompareProgressChangedEventArgs> ProgressChanged;

        protected CancellationToken CancellationToken { get { return cancellation.Token; } }

        protected readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        public SchemaComparePublishChangesOperation(SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull(nameof(comparisonResult), comparisonResult);
            ComparisonResult = comparisonResult;
        }

        public abstract void Execute();

        protected void OnProgressChanged(object sender, SchemaCompareProgressChangedEventArgs e)
            => ProgressChanged?.Invoke(sender, e);

        public void Cancel()
        {
            cancellation.Cancel();
        }

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
