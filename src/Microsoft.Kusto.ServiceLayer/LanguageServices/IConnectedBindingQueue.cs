using System;
using System.Threading;
using Microsoft.Kusto.ServiceLayer.Connection;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    public interface IConnectedBindingQueue
    {
        event BindingQueue<ConnectedBindingContext>.UnhandledExceptionDelegate OnUnhandledException;

        string AddConnectionContext(ConnectionInfo connInfo, bool needMetadata, string featureName = null, bool overwrite = false);
        void Dispose();
        bool IsBindingContextConnected(string key);
        void RemoveBindingContext(ConnectionInfo connInfo);

        QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object> bindOperation,
            Func<IBindingContext, object> timeoutOperation = null,
            Func<Exception, object> errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null);
    }
}