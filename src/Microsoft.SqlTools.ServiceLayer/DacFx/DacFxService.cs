using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    class DacFxService
    {

        private static readonly Lazy<DacFxService> instance = new Lazy<DacFxService>(() => new DacFxService());
        private readonly Lazy<ConcurrentDictionary<string, DacFxOperation>> operations =
            new Lazy<ConcurrentDictionary<string, DacFxOperation>>(() => new ConcurrentDictionary<string, DacFxOperation>());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DacFxService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(DacFxExportRequest.Type, this.HandleExportRequest);
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleExportRequest(DacFxExportParams parameters, RequestContext<DacFxExportResult> requestContext)
        {
            try
            {
                DacFxExportOperation operation = new DacFxExportOperation(parameters);
                await Task.Run(async () =>
                {
                    try
                    {
                        this.ActiveOperations[operation.OperationId] = operation;
                        operation.Execute();
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                    finally
                    {
                        DacFxOperation temp;
                        this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                    }
                });

                await requestContext.SendResult(new DacFxExportResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = ""
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to import a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleImportRequest(DacFxImportParams parameters, RequestContext<DacFxImportResult> requestContext)
        {
            try
            {
                DacFxImportOperation operation = new DacFxImportOperation(parameters);
                await Task.Run(async () =>
                {
                    try
                    {
                        this.ActiveOperations[operation.OperationId] = operation;
                        operation.Execute();
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                    finally
                    {
                        DacFxOperation temp;
                        this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                    }
                });

                await requestContext.SendResult(new DacFxImportResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = ""
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }
    }
}
