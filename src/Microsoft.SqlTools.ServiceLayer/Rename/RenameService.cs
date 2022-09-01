using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    public class RenameService : IDisposable
    {
        private bool disposed = false;

        private static Lazy<RenameService> renameServiceInstance = new Lazy<RenameService>(() => new RenameService());

        private IProtocolEndpoint ServiceHost { get; set; }

        public RenameService() { }

        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(ProcessRenameEditRequest.Type, HandleProcessRenameEditRequest);

        }
        private Task HandleRequest<T>(RequestContext<T> requestContext, Func<Task> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
            return Task.CompletedTask;
        }

        private Task HandleProcessRenameEditRequest(ProcessRenameEditRequestParams requestParams, RequestContext<bool> requestContext)
        {
            return this.HandleRequest<bool>(requestContext, async () =>
            {
                await requestContext.SendResult(true);
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}