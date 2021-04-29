using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.DataContracts.Scripting;
using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.AzureMonitor.ServiceLayer.Scripting
{
    public class ScriptingService
    {
        private static readonly Lazy<ScriptingService> LazyInstance = new Lazy<ScriptingService>(() => new ScriptingService());
        public static ScriptingService Instance => LazyInstance.Value;

        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ScriptingRequest.Type, HandleScriptExecuteRequest);
            serviceHost.SetRequestHandler(ScriptingCancelRequest.Type, HandleScriptCancelRequest);
            serviceHost.SetRequestHandler(ScriptingListObjectsRequest.Type, HandleListObjectsRequest);
        }

        private async Task HandleScriptExecuteRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try
            {
                var result = new ScriptingResult();
                Parallel.Invoke(async () => result = await ScriptExecute(parameters, requestContext));

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private async Task<ScriptingResult> ScriptExecute(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            string script = GenerateScript(parameters);
            var operationId = Guid.NewGuid().ToString();

            await requestContext.SendEvent(ScriptingCompleteEvent.Type, new ScriptingCompleteParams
            {
                Success = true,
                OperationId = operationId
            });

            await requestContext.SendEvent(ScriptingPlanNotificationEvent.Type, new ScriptingPlanNotificationParams
            {
                OperationId = operationId,
                ScriptingObjects = parameters.ScriptingObjects,
                Count = 1
            });

            return new ScriptingResult
            {
                OperationId = operationId,
                Script = script
            };
        }

        private string GenerateScript(ScriptingParams parameters)
        {
            if (parameters.Operation != ScriptingOperationType.Select || parameters.ScriptingObjects.Count > 1)
            {
                throw new InvalidOperationException("Unable to create script.");
            }

            var tableName = parameters.ScriptingObjects.First().Name;
            return $"{tableName} | take 10";
        }

        private async Task HandleScriptCancelRequest(ScriptingCancelParams parameters, RequestContext<ScriptingCancelResult> requestContext)
        {
            try
            {
                await requestContext.SendResult(new ScriptingCancelResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private Task HandleListObjectsRequest(ScriptingListObjectsParams parameters,
            RequestContext<ScriptingListObjectsResult> requestContext)
        {
            throw new NotImplementedException();
        }
    }
}