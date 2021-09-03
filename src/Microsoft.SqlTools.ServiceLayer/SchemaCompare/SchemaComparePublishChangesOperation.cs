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
            Validate.IsNotNull("comparisonResult", comparisonResult);
            ComparisonResult = comparisonResult;
        }
        
        public abstract void Execute(TaskExecutionMode mode);

        // The schema compare public api doesn't currently take a cancellation token so the operation can't be cancelled
        public void Cancel()
        {
            cancellation.Cancel();
        }
    }
}
