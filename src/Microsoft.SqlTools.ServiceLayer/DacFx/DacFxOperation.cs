using System;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Base class for DacFx operations
    /// </summary>
    abstract class DacFxOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        protected DacFxOperation()
        {
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Excecutes the DacFx operation.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Cancels the scripting operation.
        /// </summary>
        public virtual void Cancel()
        {
            if (!this.cancellation.IsCancellationRequested)
            {
                this.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Disposes the operation.
        /// </summary>
        public abstract void Dispose();
    }
}
