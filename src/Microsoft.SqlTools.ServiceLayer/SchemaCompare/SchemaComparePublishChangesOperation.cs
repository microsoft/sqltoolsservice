//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Threading;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    abstract class SchemaComparePublishChangesOperation : ITaskOperation
    {
        public string OperationId { get; private set; }

        public SqlTask SqlTask { get; set; }

        public SchemaComparisonResult ComparisonResult { get; set; }

        public string ErrorMessage { get; set; }

        protected CancellationToken CancellationToken { get { return cancellation.Token; } }

        protected readonly CancellationTokenSource cancellation = new();

        public SchemaComparePublishChangesOperation(SchemaComparisonResult comparisonResult)
        {
            Validate.IsNotNull(nameof(comparisonResult), comparisonResult);
            ComparisonResult = comparisonResult;
        }
        
        public abstract void Execute(TaskExecutionMode mode);

        public void Cancel()
        {
            cancellation.Cancel();
        }
    }
}
